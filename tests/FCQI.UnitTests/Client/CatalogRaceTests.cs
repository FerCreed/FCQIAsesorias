using FCQI.Web.Models;
using FCQI.Web.Services;
using FCQI.Web.ViewModels;

namespace FCQI.UnitTests.Client;

/// <summary>Con red lenta, una respuesta vieja no debe mezclarse con la selección actual.</summary>
public class CatalogRaceTests
{
    private sealed class SlowCatalog : ICatalogApi
    {
        public Dictionary<int, TaskCompletionSource<List<AdvisorItem>>> Pending { get; } = [];

        public Task<List<AdvisorItem>> GetAdvisorsAsync(int subjectId)
            => (Pending[subjectId] = new TaskCompletionSource<List<AdvisorItem>>()).Task;

        public Task<List<SubjectItem>> GetSubjectsAsync() => Task.FromResult(new List<SubjectItem>());
        public Task<List<AvailabilityItem>> GetAvailabilitiesAsync(int advisorId) => Task.FromResult(new List<AvailabilityItem>());
        public Task<List<LocationItem>> GetLocationsAsync() => Task.FromResult(new List<LocationItem>());
    }

    [Fact]
    public void LaRespuestaTardiaDeOtraMateria_SeDescarta()
    {
        var catalog = new SlowCatalog();
        var vm = new CatalogViewModel(new AppStatus(), catalog, null!, new SessionContext(), null!);
        var algebra = new SubjectItem { Id = 1, Name = "Álgebra" };
        var calculo = new SubjectItem { Id = 2, Name = "Cálculo" };

        vm.SelectedSubject = algebra;
        vm.SelectedSubject = calculo;
        catalog.Pending[2].SetResult([new AdvisorItem { Id = 20, FullName = "Tutora de cálculo" }]);
        catalog.Pending[1].SetResult([new AdvisorItem { Id = 10, FullName = "Tutor de álgebra" }]);

        Assert.Equal([20], vm.Advisors.Select(a => a.Id));
    }
}
