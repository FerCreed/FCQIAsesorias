using FCQI.Application.Subjects.Queries;
using FCQI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FCQI.UnitTests.Persistence;

public class GetSubjectsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldMapSeededEntitiesToDtos()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        var handler = new GetSubjectsQueryHandler(context);
        var result = await handler.HandleAsync();

        Assert.True(result.Count >= 5);
        Assert.Contains(result, s => s.Code == "MAT101" && s.Name == "Matemáticas I" && s.Program == "Tronco Común");
        Assert.Equal(result.Select(s => s.Code), result.Select(s => s.Code).OrderBy(c => c));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
