using FCQI.Domain.Entities;
using FCQI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FCQI.UnitTests.Persistence;

public class ModelMappingTests
{
    [Theory]
    [InlineData(typeof(Person), "personas")]
    [InlineData(typeof(StudentProfile), "perfiles_alumno")]
    [InlineData(typeof(AdvisorProfile), "perfiles_asesor")]
    [InlineData(typeof(AdminProfile), "perfiles_directivo")]
    [InlineData(typeof(AcademicProgram), "programas")]
    [InlineData(typeof(AcademicTerm), "ciclos_escolares")]
    [InlineData(typeof(Modality), "modalidades")]
    [InlineData(typeof(SessionStatus), "estados_sesion")]
    [InlineData(typeof(Location), "lugares")]
    [InlineData(typeof(Subject), "materias")]
    [InlineData(typeof(ProgramSubject), "programas_materias")]
    [InlineData(typeof(AdvisorSubject), "asesores_materias")]
    [InlineData(typeof(Availability), "horarios")]
    [InlineData(typeof(AdvisorySession), "asesorias")]
    public void Entity_MapsToItsTable(Type clrType, string table)
    {
        using var context = TestContext.Create();
        var entity = context.Model.FindEntityType(clrType);

        Assert.NotNull(entity);
        Assert.Equal(table, entity.GetTableName());
    }

    // Las clases del dominio están en inglés y las columnas de la base en
    // español, así que el mapeo es explícito y conviene comprobarlo: sin
    // HasColumnName, EF Core buscaría una columna inexistente y la consulta
    // solo fallaría en tiempo de ejecución, contra MySQL.
    [Theory]
    [InlineData(typeof(Person), nameof(Person.Email), "Correo")]
    [InlineData(typeof(Person), nameof(Person.DisplayName), "NombreCompleto")]
    [InlineData(typeof(StudentProfile), nameof(StudentProfile.StudentNumber), "Matricula")]
    [InlineData(typeof(AdvisorProfile), nameof(AdvisorProfile.DefaultModalityId), "ModalidadPredeterminadaId")]
    [InlineData(typeof(AdminProfile), nameof(AdminProfile.Title), "Cargo")]
    [InlineData(typeof(Availability), nameof(Availability.MaxCapacity), "CupoMaximo")]
    [InlineData(typeof(Availability), nameof(Availability.DayOfWeek), "DiaSemana")]
    [InlineData(typeof(AdvisorySession), nameof(AdvisorySession.ScheduledAt), "ProgramadaEn")]
    [InlineData(typeof(AdvisorySession), nameof(AdvisorySession.ActiveAt), "ActivaEn")]
    public void Property_MapsToItsSpanishColumn(Type clrType, string property, string column)
    {
        using var context = TestContext.Create();
        var entity = context.Model.FindEntityType(clrType);

        Assert.NotNull(entity);
        var mapped = entity.FindProperty(property);

        Assert.NotNull(mapped);
        Assert.Equal(column, mapped.GetColumnName());
    }

    [Fact]
    public void NoColumn_KeepsTheEnglishNameOfItsProperty()
    {
        using var context = TestContext.Create();

        var sinTraducir = context.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.Name != "Id" && p.GetColumnName() == p.Name)
            .Select(p => $"{p.DeclaringType.ClrType.Name}.{p.Name}")
            .ToArray();

        Assert.Empty(sinTraducir);
    }

    [Fact]
    public void AdvisorSubject_IsKeyedByTerm_SoAssignmentsSurviveTheSemester()
    {
        using var context = TestContext.Create();
        var key = context.Model.FindEntityType(typeof(AdvisorSubject))!.FindPrimaryKey();

        Assert.NotNull(key);
        Assert.Equal(
            new[] { nameof(AdvisorSubject.TermId), nameof(AdvisorSubject.AdvisorId), nameof(AdvisorSubject.SubjectId) },
            key.Properties.Select(p => p.Name).OrderBy(n => n).OrderBy(n => n == "TermId" ? 0 : n == "AdvisorId" ? 1 : 2).ToArray());
    }

    [Fact]
    public void Session_ReferencesAvailabilityByCompositeKey_SoTheAdvisorCannotBeContradicted()
    {
        using var context = TestContext.Create();
        var entity = context.Model.FindEntityType(typeof(AdvisorySession))!;

        var fk = entity.GetForeignKeys().SingleOrDefault(f =>
            f.Properties.Count == 2
            && f.Properties.Any(p => p.Name == nameof(AdvisorySession.AvailabilityId))
            && f.Properties.Any(p => p.Name == nameof(AdvisorySession.AdvisorId)));

        Assert.NotNull(fk);
        Assert.Equal(typeof(Availability), fk.PrincipalEntityType.ClrType);
    }

    [Fact]
    public void Session_AvailabilityIsRequired_BecauseTheAdvisorIsValidatedAgainstIt()
    {
        using var context = TestContext.Create();
        var property = context.Model.FindEntityType(typeof(AdvisorySession))!
            .FindProperty(nameof(AdvisorySession.AvailabilityId));

        Assert.NotNull(property);
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Session_ActiveAt_IsNeverWrittenByTheApplication()
    {
        using var context = TestContext.Create();
        var property = context.Model.FindEntityType(typeof(AdvisorySession))!
            .FindProperty(nameof(AdvisorySession.ActiveAt));

        Assert.NotNull(property);
        Assert.Equal(
            Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore,
            property.GetAfterSaveBehavior());
    }

    [Fact]
    public void Person_DisplayName_IsGeneratedByTheDatabase()
    {
        using var context = TestContext.Create();
        var property = context.Model.FindEntityType(typeof(Person))!
            .FindProperty(nameof(Person.DisplayName));

        Assert.NotNull(property);
        Assert.Equal(
            Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore,
            property.GetAfterSaveBehavior());
    }
}

internal static class TestContext
{
    public static AppDbContext Create() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
