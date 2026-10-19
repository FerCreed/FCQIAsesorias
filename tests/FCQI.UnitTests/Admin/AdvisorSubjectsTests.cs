using FCQI.Application.Administration;
using FCQI.Application.Common;
using FCQI.Domain.Entities;
using FCQI.Infrastructure.Persistence;
using FCQI.UnitTests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FCQI.UnitTests.Admin;

/// <summary>
/// El panel de dirección manda una lista de ids de materia. Antes esa lista
/// llegaba sin revisar hasta el INSERT: un id inexistente lo rechazaba la
/// llave foránea y la API respondía 500 con el volcado de la excepción.
/// </summary>
public class AdvisorSubjectsTests
{
    private const int Tutor = 1;
    private const short Ciclo = 1;

    [Fact]
    public async Task MateriaInexistente_SeRechazaConUnMensaje()
    {
        await using var db = await SeedAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Handler(db).HandleAsync(Tutor, [1, 9999]));

        Assert.Contains("9999", ex.Message);
        // Nada se guardó: la comprobación va antes de tocar la base.
        Assert.Empty(db.AdvisorSubjects);
    }

    [Fact]
    public async Task MateriaDadaDeBaja_NoSePuedeAsignar()
    {
        await using var db = await SeedAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Handler(db).HandleAsync(Tutor, [3]));

        Assert.Contains("baja", ex.Message);
    }

    [Fact]
    public async Task TutorInexistente_SeRechaza()
    {
        await using var db = await SeedAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Handler(db).HandleAsync(404, [1]));
    }

    [Fact]
    public async Task AsignacionValida_SeGuarda()
    {
        await using var db = await SeedAsync();

        await Handler(db).HandleAsync(Tutor, [1, 2]);

        var guardadas = await db.AdvisorSubjects.Select(x => x.SubjectId).OrderBy(x => x).ToListAsync();
        Assert.Equal([1, 2], guardadas);
    }

    [Fact]
    public async Task ListaVacia_DejaAlTutorSinMaterias()
    {
        await using var db = await SeedAsync();
        await Handler(db).HandleAsync(Tutor, [1, 2]);

        await Handler(db).HandleAsync(Tutor, []);

        Assert.Empty(db.AdvisorSubjects);
    }

    private static ReplaceAdvisorSubjectsCommandHandler Handler(AppDbContext db)
        => new(db, new CurrentTerm(db));

    private static async Task<AppDbContext> SeedAsync()
    {
        var db = TestContext.Create();

        db.Terms.Add(new AcademicTerm
        {
            Id = Ciclo, Code = "2026-2", Name = "Otoño 2026",
            StartsOn = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
            EndsOn = DateOnly.FromDateTime(DateTime.Today.AddDays(90)),
            IsCurrent = true
        });
        db.Programs.Add(new AcademicProgram { Id = 1, Code = "IQ", Name = "Ingeniería Química" });
        db.Modalities.Add(new Modality { Id = 1, Code = "IN_PERSON", Name = "Presencial" });
        db.AdvisorProfiles.Add(new AdvisorProfile { PersonId = Tutor, ProgramId = 1, DefaultModalityId = 1, IsActive = true });
        db.Subjects.AddRange(
            new Subject { Id = 1, Code = "CALCULO-DIFERENCIAL", Name = "Cálculo Diferencial", IsActive = true },
            new Subject { Id = 2, Code = "ALGEBRA-LINEAL", Name = "Álgebra Lineal", IsActive = true },
            new Subject { Id = 3, Code = "MATERIA-RETIRADA", Name = "Materia retirada del plan", IsActive = false });

        await db.SaveChangesAsync();
        return db;
    }
}
