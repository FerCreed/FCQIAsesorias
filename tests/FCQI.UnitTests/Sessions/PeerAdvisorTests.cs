using FCQI.Application.Common;
using FCQI.Application.Sessions.Commands;
using FCQI.Application.Sessions.Dtos;
using FCQI.Application.Sessions.Queries;
using FCQI.Domain;
using FCQI.Domain.Entities;
using FCQI.Infrastructure.Persistence;
using FCQI.Infrastructure.Time;
using FCQI.UnitTests.Persistence;

namespace FCQI.UnitTests.Sessions;

/// <summary>
/// El asesor par —un alumno que además asesora— es el caso que dio origen al
/// rediseño del modelo. Estas pruebas fijan lo que debe poder hacer con sus
/// dos roles viajando en el mismo token, sin volver a autenticarse.
/// </summary>
public class PeerAdvisorTests
{
    private const int Titular = 1;      // asesor de planta
    private const int Par = 10;         // asesor par: asesora y estudia
    private const int Alumna = 15;      // solo alumna
    private const short Ciclo = 1;
    private const int MateriaDelTitular = 1;
    private const int MateriaDelPar = 2;
    private const int BloqueDelTitular = 1;
    private const int BloqueDelPar = 2;

    /// <summary>Los dos roles en el mismo llamante, como llegan del JWT.</summary>
    private static Caller ComoAsesorPar() => new(Par, false, true, true);

    [Fact]
    public async Task AsesorPar_PuedeAgendarComoAlumno_ConOtroTutor()
    {
        await using var db = await SeedAsync();

        var dto = await Create(db).HandleAsync(
            Solicitud(advisorId: Titular, subjectId: MateriaDelTitular, availabilityId: BloqueDelTitular),
            ComoAsesorPar());

        // La identidad sale del token: la asesoría queda a nombre del par.
        Assert.Equal(Par, dto.StudentId);
        Assert.Equal(Titular, dto.AdvisorId);
        Assert.Equal(SessionStatuses.Pending, dto.Status);
    }

    [Fact]
    public async Task AsesorPar_NoPuedeAgendarseConsigoMismo()
    {
        await using var db = await SeedAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Create(db).HandleAsync(
                Solicitud(advisorId: Par, subjectId: MateriaDelPar, availabilityId: BloqueDelPar),
                ComoAsesorPar()));

        Assert.Contains("contigo mismo", ex.Message);
    }

    [Fact]
    public async Task AsesorPar_VeSusAsesoriasComoAlumno_YComoAsesor_PorSeparado()
    {
        await using var db = await SeedAsync();
        var query = new GetSessionsQueryHandler(db, new CampusClock());

        var comoAlumno = await query.HandleAsync(Par, null, null, ComoAsesorPar());
        var comoAsesor = await query.HandleAsync(null, Par, null, ComoAsesorPar());

        // La misma persona, dos bandejas distintas y ninguna mezclada.
        Assert.All(comoAlumno, s => Assert.Equal(Par, s.StudentId));
        Assert.All(comoAsesor, s => Assert.Equal(Par, s.AdvisorId));
        Assert.NotEmpty(comoAlumno);
        Assert.NotEmpty(comoAsesor);
        Assert.Empty(comoAlumno.Select(s => s.Id).Intersect(comoAsesor.Select(s => s.Id)));
    }

    [Fact]
    public async Task AsesorPar_SigueSinPoderVerLasAsesoriasDeOtros()
    {
        await using var db = await SeedAsync();
        var query = new GetSessionsQueryHandler(db, new CampusClock());

        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => query.HandleAsync(Alumna, null, null, ComoAsesorPar()));
    }

    [Fact]
    public async Task AsesorPar_ConfirmaLoQueAsesora_PeroSoloCancelaLoQueCursa()
    {
        await using var db = await SeedAsync();
        var handler = new UpdateSessionStatusCommandHandler(db);

        // Sesión 2: él es el asesor. Puede confirmarla.
        await handler.HandleAsync(2, SessionStatuses.Confirmed, ComoAsesorPar());
        Assert.Equal(SessionStatusIds.Confirmed, db.AdvisorySessions.Single(s => s.Id == 2).StatusId);

        // Sesión 3: él es el alumno. Confirmar no le toca.
        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => handler.HandleAsync(3, SessionStatuses.Confirmed, ComoAsesorPar()));

        // Pero darse de baja, sí.
        await handler.HandleAsync(3, SessionStatuses.Cancelled, ComoAsesorPar());
        Assert.Equal(SessionStatusIds.Cancelled, db.AdvisorySessions.Single(s => s.Id == 3).StatusId);
    }

    private static CreateSessionRequest Solicitud(int advisorId, int subjectId, int availabilityId)
        => new(StudentId: 0, AdvisorId: advisorId, SubjectId: subjectId,
               AvailabilityId: availabilityId, ScheduledAt: Cita(), Topic: "Prueba");

    private static CreateSessionCommandHandler Create(AppDbContext db)
        => new(db, new CurrentTerm(db), new CampusClock());

    /// <summary>
    /// Fecha de la cita: dentro de tres días a las 12:00, hora del campus. Se
    /// calcula en lugar de fijarse para que la prueba no caduque al pasar el
    /// ciclo escolar, igual que la ventana del ciclo que se siembra abajo.
    /// </summary>
    private static DateTime Cita()
        => DateTime.Today.AddDays(3).AddHours(12);

    private static async Task<AppDbContext> SeedAsync()
    {
        var db = TestContext.Create();
        var cita = Cita();

        db.SessionStatuses.AddRange(
            new SessionStatus { Id = 1, Code = "PENDING", Name = SessionStatuses.Pending, IsActive = true },
            new SessionStatus { Id = 2, Code = "CONFIRMED", Name = SessionStatuses.Confirmed, IsActive = true },
            new SessionStatus { Id = 3, Code = "CANCELLED", Name = SessionStatuses.Cancelled, IsActive = false },
            new SessionStatus { Id = 4, Code = "REJECTED", Name = SessionStatuses.Rejected, IsActive = false });

        db.Terms.Add(new AcademicTerm
        {
            Id = Ciclo, Code = "2026-2", Name = "Otoño 2026",
            StartsOn = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
            EndsOn = DateOnly.FromDateTime(DateTime.Today.AddDays(90)),
            IsCurrent = true
        });
        db.Programs.Add(new AcademicProgram { Id = 1, Code = "QI", Name = "Químico Industrial" });
        db.Modalities.Add(new Modality { Id = 1, Code = "IN_PERSON", Name = "Presencial" });
        db.Locations.Add(new Location { Id = 1, Name = "Cubículo FCQI", ModalityId = 1 });
        db.Subjects.AddRange(
            new Subject { Id = MateriaDelTitular, Code = "CALCULO-DIFERENCIAL", Name = "Cálculo Diferencial", IsActive = true },
            new Subject { Id = MateriaDelPar, Code = "QUIMICA-ANALITICA", Name = "Química Analítica", IsActive = true });

        db.People.AddRange(
            Person(Titular, "Felipe", "Márquez", "felipe.marquez63@uabc.edu.mx"),
            Person(Par, "Vladimir", "Ramírez", "v1299027@uabc.edu.mx"),
            Person(Alumna, "Yesua", "Díaz", "yesua.diaz@uabc.edu.mx"));

        // El par tiene LOS DOS perfiles: esa es la premisa del caso.
        db.AdvisorProfiles.AddRange(
            new AdvisorProfile { PersonId = Titular, ProgramId = 1, DefaultModalityId = 1, IsActive = true },
            new AdvisorProfile { PersonId = Par, ProgramId = 1, DefaultModalityId = 1, IsActive = true });
        db.StudentProfiles.AddRange(
            new StudentProfile { PersonId = Par, StudentNumber = "1299027", IsActive = true },
            new StudentProfile { PersonId = Alumna, StudentNumber = "2208134", IsActive = true });

        db.AdvisorSubjects.AddRange(
            new AdvisorSubject { TermId = Ciclo, AdvisorId = Titular, SubjectId = MateriaDelTitular },
            new AdvisorSubject { TermId = Ciclo, AdvisorId = Par, SubjectId = MateriaDelPar });

        db.Availabilities.AddRange(
            new Availability
            {
                Id = BloqueDelTitular, TermId = Ciclo, AdvisorId = Titular, DayOfWeek = cita.DayOfWeek,
                StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(16, 0), MaxCapacity = 2, LocationId = 1, IsActive = true
            },
            new Availability
            {
                Id = BloqueDelPar, TermId = Ciclo, AdvisorId = Par, DayOfWeek = cita.DayOfWeek,
                StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(16, 0), MaxCapacity = 2, LocationId = 1, IsActive = true
            });

        // Sesión 2: el par como ASESOR. Sesión 3: el par como ALUMNO.
        db.AdvisorySessions.AddRange(
            new AdvisorySession
            {
                Id = 2, TermId = Ciclo, AvailabilityId = BloqueDelPar, AdvisorId = Par,
                StudentId = Alumna, SubjectId = MateriaDelPar,
                ScheduledAt = cita.AddDays(7).ToUniversalTime(), SeatNumber = 1,
                StatusId = SessionStatusIds.Pending, Topic = "Le asesora"
            },
            new AdvisorySession
            {
                Id = 3, TermId = Ciclo, AvailabilityId = BloqueDelTitular, AdvisorId = Titular,
                StudentId = Par, SubjectId = MateriaDelTitular,
                ScheduledAt = cita.AddDays(14).ToUniversalTime(), SeatNumber = 1,
                StatusId = SessionStatusIds.Pending, Topic = "Le asesoran"
            });

        await db.SaveChangesAsync();
        return db;
    }

    private static Person Person(int id, string first, string paternal, string email)
    {
        var person = new Person
        {
            Id = id, FirstName = first, LastNamePaternal = paternal,
            LastNameMaternal = null, Email = email, IsActive = true
        };

        // DisplayName lo genera MySQL; el proveedor en memoria no lo calcula.
        typeof(Person).GetProperty(nameof(Domain.Entities.Person.DisplayName))!
            .SetValue(person, $"{first} {paternal}");

        return person;
    }
}
