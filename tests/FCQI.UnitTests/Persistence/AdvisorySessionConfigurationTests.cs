using FCQI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FCQI.UnitTests.Persistence;

public class AdvisorySessionConfigurationTests
{
    [Fact]
    public void AdvisorySession_ShouldRequireForeignKeys()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Domain.Entities.AdvisorySession));

        Assert.NotNull(entity);

        var studentFk = entity.GetForeignKeys().SingleOrDefault(fk => fk.Properties.Any(p => p.Name == nameof(Domain.Entities.AdvisorySession.StudentId)));
        var advisorFk = entity.GetForeignKeys().SingleOrDefault(fk => fk.Properties.Any(p => p.Name == nameof(Domain.Entities.AdvisorySession.AdvisorId)));
        var subjectFk = entity.GetForeignKeys().SingleOrDefault(fk => fk.Properties.Any(p => p.Name == nameof(Domain.Entities.AdvisorySession.SubjectId)));

        Assert.NotNull(studentFk);
        Assert.NotNull(advisorFk);
        Assert.NotNull(subjectFk);
        Assert.Equal(DeleteBehavior.Restrict, studentFk.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, advisorFk.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, subjectFk.DeleteBehavior);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
