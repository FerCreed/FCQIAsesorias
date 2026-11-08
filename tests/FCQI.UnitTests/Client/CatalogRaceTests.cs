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
        public Task<TermItem> GetTermAsync() => Task.FromResult(new TermItem());
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
        catalog.Pending[2].SetResult([new AdvisorItem { Id = 20, FullName = "Asesora de cálculo" }]);
        catalog.Pending[1].SetResult([new AdvisorItem { Id = 10, FullName = "Asesor de álgebra" }]);

        Assert.Equal([20], vm.Advisors.Select(a => a.Id));
    }

    [Fact]
    public void AlLlegarLosAsesores_LaMateriaElegidaSeConserva()
    {
        var catalog = new SlowCatalog();
        var vm = new CatalogViewModel(new AppStatus(), catalog, null!, new SessionContext(), null!);
        var algebra = new SubjectItem { Id = 1, Name = "Álgebra", AdvisorCount = 1 };
        vm.Subjects.Add(algebra);

        vm.SelectedSubject = algebra;
        catalog.Pending[1].SetResult([new AdvisorItem { Id = 10, FullName = "Asesor de álgebra" }]);

        // Si la lista de materias se reconstruyera, el ListBox de la vista
        // borraría la selección y la búsqueda volvería al paso 1.
        Assert.Contains(algebra, vm.Subjects);
        Assert.Same(algebra, vm.SelectedSubject);
        Assert.Equal(CatalogStep.Advisor, vm.Step);
        Assert.Single(vm.Advisors);
    }
}
