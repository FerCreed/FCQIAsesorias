using FCQI.Domain.Entities;
using FCQI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FCQI.UnitTests.Persistence;

public class ModelMappingTests
{
    [Theory]
    [InlineData(typeof(Person), "people")]
    [InlineData(typeof(StudentProfile), "student_profiles")]
    [InlineData(typeof(AdvisorProfile), "advisor_profiles")]
    [InlineData(typeof(AdminProfile), "admin_profiles")]
    [InlineData(typeof(AcademicProgram), "programs")]
    [InlineData(typeof(AcademicTerm), "academic_terms")]
    [InlineData(typeof(Modality), "modalities")]
    [InlineData(typeof(SessionStatus), "session_statuses")]
    [InlineData(typeof(Location), "locations")]
    [InlineData(typeof(Subject), "subjects")]
    [InlineData(typeof(ProgramSubject), "program_subjects")]
    [InlineData(typeof(AdvisorSubject), "advisor_subjects")]
    [InlineData(typeof(Availability), "availabilities")]
    [InlineData(typeof(AdvisorySession), "advisory_sessions")]
    public void Entity_MapsToItsTable(Type clrType, string table)
    {
        using var context = TestContext.Create();
        var entity = context.Model.FindEntityType(clrType);

        Assert.NotNull(entity);
        Assert.Equal(table, entity.GetTableName());
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
