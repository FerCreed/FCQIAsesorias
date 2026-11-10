using FCQI.Web;
using FCQI.Web.Models;
using FCQI.Web.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FCQI.UnitTests.Client;

/// <summary>Filtros del catálogo y limpieza al salir (BPMN 17).</summary>
public class FilterAndLogoutTests
{
    [Theory]
    [InlineData(TimeBand.Morning, 9, true)]
    [InlineData(TimeBand.Morning, 12, false)]
    [InlineData(TimeBand.Afternoon, 12, true)]
    [InlineData(TimeBand.Afternoon, 17, false)]
    [InlineData(TimeBand.Evening, 17, true)]
    [InlineData(TimeBand.Any, 23, true)]
    public void FranjaHoraria(TimeBand band, int hour, bool expected) => Assert.Equal(expected, CatalogFilters.Matches(band, hour));

    [Fact]
    public void Dias_SinMarcarNoFiltran_YConMarcaBastaUno()
    {
        Assert.True(CatalogFilters.Matches([], [3]));
        Assert.True(CatalogFilters.Matches([1, 3], [3, 5]));
        Assert.False(CatalogFilters.Matches([2], [3, 5]));
    }

    [Fact]
    public void TipoDeAsesor()
    {
        Assert.True(CatalogFilters.Matches(CatalogFilters.All, "Estudiante"));
        Assert.True(CatalogFilters.Matches("Profesor", "profesor"));
        Assert.False(CatalogFilters.Matches("Profesor", "Estudiante"));
    }

    [Fact]
    public void ElCalendarioSoloOfreceElDiaDelBloque()
    {
        var dates = CatalogFilters.DatesOn(3, new DateTime(2026, 10, 1), new DateTime(2026, 10, 31)).ToList();

        Assert.Equal(4, dates.Count);
        Assert.All(dates, d => Assert.Equal(DayOfWeek.Wednesday, d.DayOfWeek));
    }

    [Fact]
    public async Task AlSalir_SeVacianListasYSelecciones()
    {
        using var services = AppServices.Build(new Uri("http://localhost:1"), () => null);
        var shell = services.GetRequiredService<ShellViewModel>();
        var catalog = services.GetRequiredService<CatalogViewModel>();
        var sessions = services.GetRequiredService<SessionsViewModel>();
        catalog.Topic = "Dudas de un alumno anterior";
        sessions.Sessions.Add(new SessionItem { Id = 1, SubjectName = "Cálculo" });

        await shell.LogoutCommand.ExecuteAsync(null);

        Assert.Empty(catalog.Topic);
        Assert.Empty(sessions.Sessions);
        Assert.IsType<HomeViewModel>(shell.CurrentPage);
    }
}
