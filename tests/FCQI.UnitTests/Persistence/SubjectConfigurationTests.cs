using FCQI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FCQI.UnitTests.Persistence;

public class SubjectConfigurationTests
{
    [Fact]
    public void SubjectConfiguration_ShouldMapTableAndColumns()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Domain.Entities.Subject));

        Assert.NotNull(entity);
        Assert.Equal("subjects", entity.GetTableName());

        var code = entity.FindProperty(nameof(Domain.Entities.Subject.Code));
        Assert.NotNull(code);
        Assert.Equal(20, code.GetMaxLength());
        Assert.False(code.IsNullable);

        var name = entity.FindProperty(nameof(Domain.Entities.Subject.Name));
        Assert.NotNull(name);
        Assert.Equal(200, name.GetMaxLength());
    }

    [Fact]
    public async Task AppDbContext_ShouldSeedSubjects()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var subjects = await context.Subjects.OrderBy(s => s.Code).ToListAsync();

        Assert.True(subjects.Count >= 5);
        Assert.Contains(subjects, s => s.Code == "MAT101");
        Assert.Contains(subjects, s => s.Name == "Matemáticas I");
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
