using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Models;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

public enum CatalogStep
{
    Subject,
    Advisor,
    Slot
}

/// <summary>
/// Catálogo público en tres pasos: materia (BPMN 5), asesor (BPMN 4) y horario
/// (BPMN 3); al final, solicitar (BPMN 8) o escribir al asesor (BPMN 15).
/// Se consulta sin cuenta; solicitar pide entrar y retoma la solicitud.
/// </summary>
public partial class CatalogViewModel : PageViewModel
{
    private readonly ICatalogApi _catalog;
    private readonly IFcqiApi _api;
    private readonly SessionContext _session;
    private readonly INavigator _navigator;
    private List<SubjectItem> _allSubjects = [];
    private bool _resumeRequest;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step), nameof(Heading), nameof(Context), nameof(CanGoBack))]
    private SubjectItem? _selectedSubject;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step), nameof(Heading), nameof(Context))]
    private AdvisorItem? _selectedAdvisor;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSlotSelected))]
    private AvailabilityItem? _selectedSlot;

    [ObservableProperty] private string _query = string.Empty;
    [ObservableProperty] private string _topic = string.Empty;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private bool _isContactOpen;

    public CatalogViewModel(AppStatus status, ICatalogApi catalog, IFcqiApi api, SessionContext session, INavigator navigator)
        : base(status)
    {
        _catalog = catalog;
        _api = api;
        _session = session;
        _navigator = navigator;
        _session.PropertyChanged += (_, _) => OnPropertyChanged(nameof(RequestLabel));
    }

    public override string Title => "Catálogo";

    public ObservableCollection<SubjectItem> Subjects { get; } = [];
    public ObservableCollection<AdvisorItem> Advisors { get; } = [];
    public ObservableCollection<AvailabilityItem> Slots { get; } = [];

    public CatalogStep Step => SelectedAdvisor is not null ? CatalogStep.Slot
        : SelectedSubject is not null ? CatalogStep.Advisor
        : CatalogStep.Subject;

    public bool IsSubjectStep => Step == CatalogStep.Subject;
    public bool IsAdvisorStep => Step == CatalogStep.Advisor;
    public bool IsSlotStep => Step == CatalogStep.Slot;
    public bool CanGoBack => SelectedSubject is not null;
    public bool HasSlotSelected => SelectedSlot is not null;

    public string Heading => Step switch
    {
        CatalogStep.Subject => "Elige una materia",
        CatalogStep.Advisor => "Elige un asesor",
        _ => "Elige un horario"
    };

    public string Context => Step switch
    {
        CatalogStep.Subject => "Busca por nombre o clave.",
        CatalogStep.Advisor => SelectedSubject!.Name,
        _ => $"{SelectedSubject!.Name}, con {SelectedAdvisor!.FullName}"
    };

    public string EmptyText => Step switch
    {
        CatalogStep.Subject => "Ninguna materia coincide con la búsqueda.",
        CatalogStep.Advisor => "Dirección aún no asigna asesores a esta materia. Prueba con otra.",
        _ => "Este asesor no tiene horarios publicados. Escríbele o elige otro asesor."
    };

    public bool IsEmpty => !IsBusy && Step switch
    {
        CatalogStep.Subject => Subjects.Count == 0,
        CatalogStep.Advisor => Advisors.Count == 0,
        _ => Slots.Count == 0
    };

    public string RequestLabel => _session.IsLoggedIn ? "Solicitar asesoría" : "Entrar y solicitar";

    public override async Task OnNavigatedToAsync()
    {
        if (_allSubjects.Count == 0)
        {
            await RunAsync(async () =>
            {
                _allSubjects = await _catalog.GetSubjectsAsync();
                Filter();
            });
        }

        if (_resumeRequest && _session.IsLoggedIn)
        {
            _resumeRequest = false;
            await RequestAsync();
        }
    }

    partial void OnQueryChanged(string value) => Filter();

    partial void OnSelectedSubjectChanged(SubjectItem? value)
    {
        SelectedAdvisor = null;
        Advisors.Clear();
        if (value is not null)
        {
            _ = LoadAsync(Advisors, () => _catalog.GetAdvisorsAsync(value.Id), () => SelectedSubject == value);
        }

        NotifyStep();
    }

    partial void OnSelectedAdvisorChanged(AdvisorItem? value)
    {
        SelectedSlot = null;
        IsContactOpen = false;
        Slots.Clear();
        if (value is not null)
        {
            _ = LoadAsync(Slots, () => _catalog.GetAvailabilitiesAsync(value.Id), () => SelectedAdvisor == value);
        }

        NotifyStep();
    }

    [RelayCommand]
    private void ClearSlot() => SelectedSlot = null;

    [RelayCommand]
    private void Back()
    {
        if (SelectedAdvisor is not null) SelectedAdvisor = null;
        else SelectedSubject = null;
    }

    [RelayCommand]
    private async Task RequestAsync()
    {
        if (SelectedSubject is null || SelectedAdvisor is null || SelectedSlot is null)
        {
            Status.Error("Elige materia, asesor y horario.");
            return;
        }

        if (!_session.IsLoggedIn)
        {
            _resumeRequest = true;
            _navigator.RequireLogin(AppPage.Catalog, "Entra con tu correo @uabc.edu.mx para enviar la solicitud.");
            return;
        }

        if (!_session.IsStudent)
        {
            Status.Error("Solo una cuenta de alumno puede solicitar asesorías. Si también eres alumno, cambia de rol arriba.");
            return;
        }

        var body = new CreateSessionBody(_session.PersonId, SelectedAdvisor.Id, SelectedSubject.Id, SelectedSlot.Id,
            SelectedSlot.NextOccurrence(DateTime.Today), string.IsNullOrWhiteSpace(Topic) ? "Dudas de la materia" : Topic.Trim());

        if (await RunAsync(() => _api.CreateSessionAsync(body), "Solicitud enviada. Queda pendiente hasta que el asesor la confirme."))
        {
            Topic = string.Empty;
            SelectedSlot = null;
            await _navigator.GoAsync(AppPage.Sessions);
        }
    }

    [RelayCommand]
    private void ToggleContact()
    {
        if (!_session.IsLoggedIn)
        {
            _navigator.RequireLogin(AppPage.Catalog, "Entra para escribirle al asesor.");
            return;
        }

        IsContactOpen = !IsContactOpen;
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (SelectedAdvisor is null || string.IsNullOrWhiteSpace(Message))
        {
            Status.Error("Escribe tu mensaje para el asesor.");
            return;
        }

        if (await RunAsync(() => _api.ContactAdvisorAsync(SelectedAdvisor.Id, Message.Trim()),
                $"Mensaje enviado a {SelectedAdvisor.FullName}. Te responderá a tu correo."))
        {
            Message = string.Empty;
            IsContactOpen = false;
        }
    }

    private void Filter()
    {
        var q = Query.Trim();
        Subjects.Clear();
        foreach (var s in _allSubjects.Where(s => q.Length == 0
                                                  || s.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                                                  || s.Code.Contains(q, StringComparison.OrdinalIgnoreCase)))
        {
            Subjects.Add(s);
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>
    /// Carga una lista del paso actual. No usa <see cref="PageViewModel.RunAsync"/>:
    /// su bloqueo descartaría la carga si el alumno cambia de materia mientras
    /// la anterior sigue en curso. En su lugar, la respuesta se descarta si la
    /// selección ya cambió (<paramref name="stillCurrent"/>), para no mezclar
    /// los asesores de una materia con los de otra.
    /// </summary>
    internal async Task LoadAsync<T>(ObservableCollection<T> target, Func<Task<List<T>>> fetch, Func<bool> stillCurrent)
    {
        try
        {
            var items = await fetch();
            if (!stillCurrent())
            {
                return;
            }

            target.Clear();
            foreach (var item in items)
            {
                target.Add(item);
            }
        }
        catch (Exception ex) when (stillCurrent())
        {
            Status.Error(ex.Message);
        }
        catch
        {
            // Falló una consulta que ya nadie mira; se ignora.
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    private void NotifyStep()
    {
        OnPropertyChanged(nameof(IsSubjectStep));
        OnPropertyChanged(nameof(IsAdvisorStep));
        OnPropertyChanged(nameof(IsSlotStep));
        OnPropertyChanged(nameof(EmptyText));
        OnPropertyChanged(nameof(IsEmpty));
    }
}
