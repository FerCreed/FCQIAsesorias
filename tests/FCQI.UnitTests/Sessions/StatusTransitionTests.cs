using FCQI.Application.Common;
using FCQI.Application.Sessions.Commands;
using FCQI.Domain;
using FCQI.Domain.Entities;
using FCQI.Infrastructure.Persistence;
using FCQI.UnitTests.Persistence;

namespace FCQI.UnitTests.Sessions;

/// <summary>
/// Cancelada y Rechazada son estados finales.
///
/// Sin esa regla, confirmar una asesoría cancelada la devolvía a la vida: el
/// trigger le reasignaba ActiveAt y el lugar volvía a quedar ocupado. Si otro
/// alumno ya lo había tomado, el índice único de cupo abortaba la operación y
/// la API respondía 500; y si seguía libre, una cita que el alumno canceló
/// reaparecía confirmada sin que él hiciera nada.
/// </summary>
public class StatusTransitionTests
{
    private const int Asesor = 1;
    private const int Alumno = 2;

    private static Caller ElAsesor() => new(Asesor, false, true, false);
    private static Caller ElAlumno() => new(Alumno, false, false, true);
    private static Caller Direccion() => new(99, true, false, false);

    [Theory]
    [InlineData(SessionStatusIds.Cancelled, "cancelada")]
    [InlineData(SessionStatusIds.Rejected, "rechazada")]
    public async Task EstadoFinal_NoVuelveAEstarActiva(byte final, string palabra)
    {
        await using var db = await SeedAsync(final);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new UpdateSessionStatusCommandHandler(db)
                .HandleAsync(1, SessionStatuses.Confirmed, ElAsesor()));

        Assert.Contains(palabra, ex.Message);
        Assert.Equal(final, db.AdvisorySessions.Single().StatusId);
    }

    [Fact]
    public async Task NiSiquieraDireccionRevivUnaAsesoriaCancelada()
    {
        await using var db = await SeedAsync(SessionStatusIds.Cancelled);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new UpdateSessionStatusCommandHandler(db)
                .HandleAsync(1, SessionStatuses.Pending, Direccion()));
    }

    [Fact]
    public async Task PedirElEstadoQueYaTiene_NoEsError()
    {
        await using var db = await SeedAsync(SessionStatusIds.Cancelled);

        // Una petición repetida, o dos pestañas haciendo lo mismo: no hay nada
        // que cambiar, pero tampoco motivo para fallar.
        await new UpdateSessionStatusCommandHandler(db)
            .HandleAsync(1, SessionStatuses.Cancelled, ElAlumno());

        Assert.Equal(SessionStatusIds.Cancelled, db.AdvisorySessions.Single().StatusId);
    }

    [Theory]
    [InlineData(SessionStatusIds.Pending, SessionStatuses.Confirmed, SessionStatusIds.Confirmed)]
    [InlineData(SessionStatusIds.Pending, SessionStatuses.Rejected, SessionStatusIds.Rejected)]
    [InlineData(SessionStatusIds.Confirmed, SessionStatuses.Cancelled, SessionStatusIds.Cancelled)]
    public async Task LasTransicionesDeVerdad_SiguenFuncionando(byte desde, string hacia, byte esperado)
    {
        await using var db = await SeedAsync(desde);

        await new UpdateSessionStatusCommandHandler(db).HandleAsync(1, hacia, ElAsesor());

        Assert.Equal(esperado, db.AdvisorySessions.Single().StatusId);
    }

    private static async Task<AppDbContext> SeedAsync(byte estadoInicial)
    {
        var db = TestContext.Create();

        db.SessionStatuses.AddRange(
            new SessionStatus { Id = 1, Code = "PENDING", Name = SessionStatuses.Pending, IsActive = true },
            new SessionStatus { Id = 2, Code = "CONFIRMED", Name = SessionStatuses.Confirmed, IsActive = true },
            new SessionStatus { Id = 3, Code = "CANCELLED", Name = SessionStatuses.Cancelled, IsActive = false },
            new SessionStatus { Id = 4, Code = "REJECTED", Name = SessionStatuses.Rejected, IsActive = false });

        db.AdvisorySessions.Add(new AdvisorySession
        {
            Id = 1, TermId = 1, AvailabilityId = 1, AdvisorId = Asesor, StudentId = Alumno,
            SubjectId = 1, ScheduledAt = DateTime.UtcNow.AddDays(3), SeatNumber = 1,
            StatusId = estadoInicial, Topic = "Prueba"
        });

        await db.SaveChangesAsync();
        return db;
    }
}
