using System.Collections.Specialized;
using System.Reflection;
using FCQI.Web.Models;
using FCQI.Web.Services;
using FCQI.Web.ViewModels;

namespace FCQI.UnitTests.Client;

// RF-05: la solicitud iniciada sin sesión se envía sola al volver del login,
// con el horario, la fecha y el tema que la persona había elegido.
public class PendingRequestTests
{
    private static readonly AvailabilityItem Bloque = new() { Id = 7, AdvisorId = 10, DayOfWeek = 3, StartTime = "10:00:00", EndTime = "11:00:00", Modality = "Presencial" };

    // Catálogo inmediato con una materia, un asesor y un horario.
    private sealed class FixedCatalog : ICatalogApi
    {
        public Task<List<SubjectItem>> GetSubjectsAsync() => Task.FromResult(new List<SubjectItem> { new() { Id = 1, Name = "Álgebra", AdvisorCount = 1 } });
        public Task<List<AdvisorItem>> GetAdvisorsAsync(int subjectId) => Task.FromResult(new List<AdvisorItem> { new() { Id = 10, FullName = "Asesor" } });
        public Task<List<AvailabilityItem>> GetAvailabilitiesAsync(int advisorId) => Task.FromResult(new List<AvailabilityItem> { Bloque });
        public Task<List<LocationItem>> GetLocationsAsync() => Task.FromResult(new List<LocationItem>());
        public Task<TermItem> GetTermAsync() => Task.FromResult(new TermItem { EndsOn = DateOnly.FromDateTime(DateTime.Today.AddMonths(4)) });
    }

    // Navegador que solo anota a dónde se pidió ir.
    private sealed class RecordingNavigator : INavigator
    {
        public List<AppPage> Visited { get; } = [];
        public Task GoAsync(AppPage page) { Visited.Add(page); return Task.CompletedTask; }
        public void RequireLogin(AppPage returnTo, string message) => Visited.Add(AppPage.Login);
        public Task CompleteLoginAsync() => Task.CompletedTask;
    }

    // API falsa: guarda el cuerpo de CreateSessionAsync y responde vacío a lo demás.
    public class RecordingApi : DispatchProxy
    {
        public CreateSessionBody? Created { get; private set; }

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IFcqiApi.CreateSessionAsync)) Created = (CreateSessionBody)args![0]!;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task AlVolverDelLogin_LaSolicitudPendienteSeEnvia()
    {
        var session = new SessionContext();
        var navigator = new RecordingNavigator();
        var api = DispatchProxy.Create<IFcqiApi, RecordingApi>();
        var vm = new CatalogViewModel(new AppStatus(), new FixedCatalog(), api, session, navigator);
        // Imita al ListBox de la vista: al vaciarse la lista de horarios, borra la selección.
        vm.Slots.CollectionChanged += (_, e) => { if (e.Action == NotifyCollectionChangedAction.Reset) vm.SelectedSlot = null; };

        await vm.OnNavigatedToAsync();
        vm.SelectedSubject = vm.Subjects.Single();
        vm.SelectedAdvisor = vm.Advisors.Single();
        vm.SelectedSlot = vm.Slots.Single();
        var fecha = vm.SelectedDate!.Value.AddDays(7);
        vm.SelectedDate = fecha;
        vm.Topic = "Matrices";

        await vm.RequestCommand.ExecuteAsync(null);
        Assert.Equal([AppPage.Login], navigator.Visited);

        session.SignIn(new AuthResultItem { Token = "t", Role = UserRoles.Student, ProfileId = 5 });
        await vm.OnNavigatedToAsync();

        var created = ((RecordingApi)(object)api).Created;
        Assert.NotNull(created);
        Assert.Equal(Bloque.Id, created.AvailabilityId);
        Assert.Equal(fecha.Date.AddHours(10), created.ScheduledAt);
        Assert.Equal("Matrices", created.Topic);
        Assert.Equal(AppPage.Sessions, navigator.Visited[^1]);
    }
}
