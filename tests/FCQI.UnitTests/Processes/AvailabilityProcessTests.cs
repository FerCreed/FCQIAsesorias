using FCQI.Application.Advisors.Commands;
using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Common;
using FCQI.Domain;
using FCQI.Infrastructure.Persistence;
using static FCQI.UnitTests.Processes.ProcessWorld;

namespace FCQI.UnitTests.Processes;

/// <summary>BPMN 7 (configurar), 13 (editar) y 16 (retirar) horarios.</summary>
public class AvailabilityProcessTests
{
    private static SaveAvailabilityRequest Block(DayOfWeek day, int from, int to, int capacity = 1, short location = 1)
        => new(day, new TimeOnly(from, 0), new TimeOnly(to, 0), capacity, location);

    [Fact]
    public async Task Configurar_RechazaTraslapes_YAceptaBloquesLibres()
    {
        await using var db = await SeedAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Handler(db).CreateAsync(Block(DayOfWeek.Wednesday, 13, 15), AsAdvisor()));
        Assert.Contains("traslapa", ex.Message);

        var id = await Handler(db).CreateAsync(Block(DayOfWeek.Wednesday, 14, 16), AsAdvisor());
        Assert.True(db.Availabilities.Single(a => a.Id == id).IsActive);
    }

    [Theory]
    [InlineData(14, 12, 1)]   // fin antes del inicio
    [InlineData(8, 9, 0)]     // sin cupo
    public async Task Configurar_ValidaDatos(int from, int to, int capacity)
    {
        await using var db = await SeedAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Handler(db).CreateAsync(Block(DayOfWeek.Monday, from, to, capacity), AsAdvisor()));
    }

    [Fact]
    public async Task SoloUnAsesor_ConfiguraHorarios()
    {
        await using var db = await SeedAsync();

        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => Handler(db).CreateAsync(Block(DayOfWeek.Monday, 8, 9), AsStudent()));
    }

    [Fact]
    public async Task ConCitasActivas_NoSeMueveNiSeRetira_PeroSiCreceElCupo()
    {
        await using var db = await SeedAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Handler(db).UpdateAsync(InPersonBlock, Block(DayOfWeek.Thursday, 12, 14), AsAdvisor()));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Handler(db).RetireAsync(InPersonBlock, AsAdvisor()));

        await Handler(db).UpdateAsync(InPersonBlock, Block(DayOfWeek.Wednesday, 12, 14, capacity: 3), AsAdvisor());
        Assert.Equal(3, db.Availabilities.Single(a => a.Id == InPersonBlock).MaxCapacity);
    }

    [Fact]
    public async Task Retirar_YVolverACrear_ReactivaElMismoBloque()
    {
        await using var db = await SeedAsync(SessionStatusIds.Cancelled);

        await Handler(db).RetireAsync(InPersonBlock, AsAdvisor());
        Assert.False(db.Availabilities.Single(a => a.Id == InPersonBlock).IsActive);

        var id = await Handler(db).CreateAsync(Block(DayOfWeek.Wednesday, 12, 13), AsAdvisor());
        Assert.Equal(InPersonBlock, id);
        Assert.True(db.Availabilities.Single(a => a.Id == id).IsActive);
    }

    private static AvailabilityCommandHandler Handler(AppDbContext db) => new(db, new CurrentTerm(db), Clock());
}
