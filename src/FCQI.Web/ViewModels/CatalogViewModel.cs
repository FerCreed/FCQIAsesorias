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
/// Buscar asesoría en tres pasos: materia (BPMN 5), asesor (BPMN 4) y horario
/// (BPMN 3), con filtros en cada paso (BPMN 8: materia, programa, profesor,
/// modalidad, día y horario). Al final se elige la fecha en el calendario y se
/// solicita, o se escribe al asesor (BPMN 15). Se consulta sin cuenta.
/// </summary>
public partial class CatalogViewModel : PageViewModel, IResettable
{
    private readonly ICatalogApi _catalog;
    private readonly IFcqiApi _api;
    private readonly SessionContext _session;
    private readonly INavigator _navigator;
    private List<SubjectItem> _allSubjects = [];
    private List<AdvisorItem> _allAdvisors = [];
    private List<AvailabilityItem> _allSlots = [];
    private TermItem? _term;
    private bool _resumeRequest;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step), nameof(Heading), nameof(Context), nameof(CanGoBack))]
    private SubjectItem? _selectedSubject;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step), nameof(Heading), nameof(Context))]
    private AdvisorItem? _selectedAdvisor;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSlotSelected), nameof(AllowedDates))]
    private AvailabilityItem? _selectedSlot;

    [ObservableProperty] private DateTime? _selectedDate;
    [ObservableProperty] private string _topic = string.Empty;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private bool _isContactOpen;

    // Filtros
    [ObservableProperty] private string _query = string.Empty;
    [ObservableProperty] private string _program = CatalogFilters.All;
    [ObservableProperty] private bool _onlyWithAdvisors = true;
    [ObservableProperty] private string _kind = CatalogFilters.All;
    [ObservableProperty] private string _modality = CatalogFilters.All;
    [ObservableProperty] private Option<TimeBand> _timeBand = CatalogFilters.TimeBands[0];

    public CatalogViewModel(AppStatus status, ICatalogApi catalog, IFcqiApi api, SessionContext session, INavigator navigator)
        : base(status)
    {
        _catalog = catalog;
        _api = api;
        _session = session;
        _navigator = navigator;
        Days = DayToggle.Weekdays(ApplyFilters);
        _session.PropertyChanged += (_, _) => OnPropertyChanged(nameof(RequestLabel));
    }

    public override string Title => "Buscar asesoría";

    public ObservableCollection<SubjectItem> Subjects { get; } = [];
    public ObservableCollection<AdvisorItem> Advisors { get; } = [];
    public ObservableCollection<AvailabilityItem> Slots { get; } = [];
    public ObservableCollection<string> Programs { get; } = [CatalogFilters.All];
    public IReadOnlyList<DayToggle> Days { get; }
    public IReadOnlyList<string> Kinds => CatalogFilters.Kinds;
    public IReadOnlyList<string> Modalities => CatalogFilters.Modalities;
    public IReadOnlyList<Option<TimeBand>> TimeBands => CatalogFilters.TimeBands;

    public CatalogStep Step => SelectedAdvisor is not null ? CatalogStep.Slot
        : SelectedSubject is not null ? CatalogStep.Advisor
        : CatalogStep.Subject;

    public bool IsSubjectStep => Step == CatalogStep.Subject;
    public bool IsAdvisorStep => Step == CatalogStep.Advisor;
    public bool IsSlotStep => Step == CatalogStep.Slot;
    public bool CanGoBack => SelectedSubject is not null;
    public bool HasSlotSelected => SelectedSlot is not null;

    /// <summary>Primer día que se puede pedir: mañana. Último: fin del ciclo.</summary>
    public DateTime FirstDate => DateTime.Today.AddDays(1);
    public DateTime LastDate => _term?.EndsOn.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today.AddMonths(4);

    /// <summary>Fechas del ciclo que caen en el día del bloque elegido; el calendario bloquea las demás.</summary>
    public IReadOnlyList<DateTime>? AllowedDates => SelectedSlot is null
        ? null
        : CatalogFilters.DatesOn(SelectedSlot.DayOfWeek, FirstDate, LastDate).ToList();

    public string Heading => Step switch
    {
        CatalogStep.Subject => "Elige una materia",
        CatalogStep.Advisor => "Elige un asesor",
        _ => "Elige un horario"
    };

    public string Context => Step switch
    {
        CatalogStep.Subject => "Busca por nombre o clave y filtra por programa educativo.",
        CatalogStep.Advisor => SelectedSubject!.Name,
        _ => $"{SelectedSubject!.Name}, con {SelectedAdvisor!.FullName}"
    };

    public string ResultText => Step switch
    {
        CatalogStep.Subject => $"{Subjects.Count} de {_allSubjects.Count} materias",
        CatalogStep.Advisor => $"{Advisors.Count} de {_allAdvisors.Count} asesores",
        _ => $"{Slots.Count} de {_allSlots.Count} horarios"
    };

    public string EmptyText => Step switch
    {
        CatalogStep.Subject => "Ninguna materia coincide con los filtros.",
        CatalogStep.Advisor => _allAdvisors.Count == 0
            ? "Dirección aún no asigna asesores a esta materia. Prueba con otra."
            : "Ningún asesor coincide con los filtros.",
        _ => _allSlots.Count == 0
            ? "Este asesor no tiene horarios publicados. Escríbele o elige otro asesor."
            : "Ningún horario coincide con los filtros."
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
        // Se recargan en cada visita: dirección pudo asignar asesores a una
        // materia desde la última vez, y el filtro "solo con asesores" la ocultaría.
        await RunAsync(async () =>
        {
            _term = await _catalog.GetTermAsync();
            _allSubjects = await _catalog.GetSubjectsAsync();
            foreach (var p in _allSubjects.SelectMany(s => s.Programs).Distinct().Order().Except(Programs))
            {
                Programs.Add(p);
            }

            OnPropertyChanged(nameof(LastDate));
            ApplyFilters();
        });

        if (_resumeRequest && _session.IsLoggedIn)
        {
            _resumeRequest = false;
            await RequestAsync();
        }
    }

    public void Reset()
    {
        SelectedSubject = null;
        (Topic, Message, IsContactOpen, _resumeRequest) = (string.Empty, string.Empty, false, false);
        ClearFilters();
    }

    partial void OnQueryChanged(string value) => ApplyFilters();
    partial void OnProgramChanged(string value) => ApplyFilters();
    partial void OnOnlyWithAdvisorsChanged(bool value) => ApplyFilters();
    partial void OnKindChanged(string value) => ApplyFilters();
    partial void OnModalityChanged(string value) => ApplyFilters();
    partial void OnTimeBandChanged(Option<TimeBand> value) => ApplyFilters();

    partial void OnSelectedSubjectChanged(SubjectItem? value)
    {
        SelectedAdvisor = null;
        _allAdvisors = [];
        Advisors.Clear();
        if (value is not null)
        {
            _ = LoadAsync(() => _catalog.GetAdvisorsAsync(value.Id), items => _allAdvisors = items, () => SelectedSubject == value);
        }

        NotifyStep();
    }

    partial void OnSelectedAdvisorChanged(AdvisorItem? value)
    {
        SelectedSlot = null;
        IsContactOpen = false;
        _allSlots = [];
        Slots.Clear();
        if (value is not null)
        {
            _ = LoadAsync(() => _catalog.GetAvailabilitiesAsync(value.Id), items => _allSlots = items, () => SelectedAdvisor == value);
        }

        NotifyStep();
    }

    partial void OnSelectedSlotChanged(AvailabilityItem? value)
        => SelectedDate = value?.NextOccurrence(DateTime.Today).Date;

    [RelayCommand]
    private void ClearFilters()
    {
        (Query, Program, OnlyWithAdvisors, Kind, Modality, TimeBand) =
            (string.Empty, CatalogFilters.All, true, CatalogFilters.All, CatalogFilters.All, CatalogFilters.TimeBands[0]);
        foreach (var day in Days) day.IsChecked = false;
        ApplyFilters();
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
        if (SelectedSubject is null || SelectedAdvisor is null || SelectedSlot is null || SelectedDate is null)
        {
            Status.Error("Elige materia, asesor, horario y fecha.");
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

        var start = SelectedDate.Value.Date + (TimeSpan.TryParse(SelectedSlot.StartTime, out var t) ? t : TimeSpan.Zero);
        var body = new CreateSessionBody(_session.PersonId, SelectedAdvisor.Id, SelectedSubject.Id, SelectedSlot.Id,
            start, string.IsNullOrWhiteSpace(Topic) ? "Dudas de la materia" : Topic.Trim());

        if (await RunAsync(() => _api.CreateSessionAsync(body),
                $"Solicitud enviada para el {start:dd/MM/yyyy} a las {start:HH:mm}. Queda pendiente hasta que el asesor la confirme."))
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

    /// <summary>
    /// Aplica los filtros a la lista del paso visible. Solo esa: vaciar la lista
    /// de un paso anterior haría que su ListBox borrara la selección (la materia
    /// o el asesor elegidos) y la búsqueda regresaría sola al paso 1.
    /// </summary>
    private void ApplyFilters()
    {
        var days = Days.Where(d => d.IsChecked).Select(d => d.Value).ToList();

        switch (Step)
        {
            case CatalogStep.Subject:
                Replace(Subjects, _allSubjects.Where(s =>
                    CatalogFilters.Contains(Query, s.Name, s.Code) &&
                    (Program == CatalogFilters.All || s.Programs.Contains(Program)) &&
                    (!OnlyWithAdvisors || s.AdvisorCount > 0)));
                break;
            case CatalogStep.Advisor:
                Replace(Advisors, _allAdvisors.Where(a =>
                    CatalogFilters.Matches(Kind, a.Kind) &&
                    CatalogFilters.Matches(Modality, a.DefaultModality) &&
                    CatalogFilters.Matches(days, a.Days)));
                break;
            default:
                Replace(Slots, _allSlots.Where(s =>
                    CatalogFilters.Matches(Modality, s.Modality) &&
                    CatalogFilters.Matches(TimeBand.Value, s.StartHour) &&
                    CatalogFilters.Matches(days, [s.DayOfWeek])));
                break;
        }

        OnPropertyChanged(nameof(ResultText));
        OnPropertyChanged(nameof(EmptyText));
        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>
    /// Carga una lista del paso actual. La respuesta se descarta si la selección
    /// ya cambió (<paramref name="stillCurrent"/>), para no mezclar asesores de
    /// una materia con los de otra.
    /// </summary>
    internal async Task LoadAsync<T>(Func<Task<List<T>>> fetch, Action<List<T>> store, Func<bool> stillCurrent)
    {
        try
        {
            var items = await fetch();
            if (!stillCurrent())
            {
                return;
            }

            store(items);
            ApplyFilters();
        }
        catch (Exception ex) when (stillCurrent())
        {
            Status.Error(ex.Message);
        }
        catch
        {
            // Falló una consulta que ya nadie mira; se ignora.
        }
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items) target.Add(item);
    }

    private void NotifyStep()
    {
        ApplyFilters();
        OnPropertyChanged(nameof(IsSubjectStep));
        OnPropertyChanged(nameof(IsAdvisorStep));
        OnPropertyChanged(nameof(IsSlotStep));
        OnPropertyChanged(nameof(ResultText));
        OnPropertyChanged(nameof(EmptyText));
        OnPropertyChanged(nameof(IsEmpty));
    }
}
