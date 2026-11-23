using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Models;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

// Paso visible del buscador; se deduce de lo seleccionado, no se guarda aparte.
public enum CatalogStep
{
    Subject,
    Advisor,
    Slot
}

// Buscar asesoría en tres pasos: materia (BPMN 5), asesor (BPMN 4) y horario
// (BPMN 3), con filtros en cada paso (BPMN 8: materia, programa, profesor,
// modalidad, día y horario). Al final se elige la fecha en el calendario y se
// solicita, o se escribe al asesor (BPMN 15). Se consulta sin cuenta.
public partial class CatalogViewModel : PageViewModel, IResettable
{
    private readonly ICatalogApi _catalog;
    private readonly IFcqiApi _api;
    private readonly SessionContext _session;
    private readonly INavigator _navigator;
    // Listas completas de cada paso; las colecciones observables guardan solo lo filtrado.
    private List<SubjectItem> _allSubjects = [];
    private List<AdvisorItem> _allAdvisors = [];
    private List<AvailabilityItem> _allSlots = [];
    private TermItem? _term;
    // Si la solicitud se interrumpió para iniciar sesión, se reanuda al volver.
    private bool _resumeRequest;

    // Selecciones de cada paso; al cambiar se notifican las propiedades derivadas del paso.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step), nameof(Heading), nameof(Context), nameof(CanGoBack))]
    private SubjectItem? _selectedSubject;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step), nameof(Heading), nameof(Context))]
    private AdvisorItem? _selectedAdvisor;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSlotSelected), nameof(AllowedDates))]
    private AvailabilityItem? _selectedSlot;

    // Datos de la solicitud y del mensaje al asesor.
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

    // Recibe catálogo público y API autenticada por separado: el catálogo funciona sin sesión.
    // Se suscribe a la sesión para cambiar la etiqueta del botón al entrar o salir.
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

    // Listas y opciones que enlaza la vista.
    public ObservableCollection<SubjectItem> Subjects { get; } = [];
    public ObservableCollection<AdvisorItem> Advisors { get; } = [];
    public ObservableCollection<AvailabilityItem> Slots { get; } = [];
    public ObservableCollection<string> Programs { get; } = [CatalogFilters.All];
    public IReadOnlyList<DayToggle> Days { get; }
    public IReadOnlyList<string> Kinds => CatalogFilters.Kinds;
    public IReadOnlyList<string> Modalities => CatalogFilters.Modalities;
    public IReadOnlyList<Option<TimeBand>> TimeBands => CatalogFilters.TimeBands;

    // Con asesor elegido se ve el paso de horario; con materia, el de asesor; si no, el de materia.
    public CatalogStep Step => SelectedAdvisor is not null ? CatalogStep.Slot
        : SelectedSubject is not null ? CatalogStep.Advisor
        : CatalogStep.Subject;

    public bool IsSubjectStep => Step == CatalogStep.Subject;
    public bool IsAdvisorStep => Step == CatalogStep.Advisor;
    public bool IsSlotStep => Step == CatalogStep.Slot;
    public bool CanGoBack => SelectedSubject is not null;
    public bool HasSlotSelected => SelectedSlot is not null;

    // Primer día que se puede pedir: mañana. Último: fin del ciclo.
    public DateTime FirstDate => DateTime.Today.AddDays(1);
    public DateTime LastDate => _term?.EndsOn.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today.AddMonths(4);

    // Fechas del ciclo que caen en el día del bloque elegido; el calendario bloquea las demás.
    public IReadOnlyList<DateTime>? AllowedDates => SelectedSlot is null
        ? null
        : CatalogFilters.DatesOn(SelectedSlot.DayOfWeek, FirstDate, LastDate).ToList();

    // Textos que cambian con el paso: título, contexto, conteo y mensaje de lista vacía.
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

    // Sin sesión el botón anticipa que pedirá entrar antes de solicitar.
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

        // Regreso del login: se envía la solicitud que quedó pendiente sin que la persona repita los pasos.
        if (_resumeRequest && _session.IsLoggedIn)
        {
            _resumeRequest = false;
            await RequestAsync();
        }
    }

    // Al cerrar sesión vuelve al primer paso y borra textos y filtros.
    public void Reset()
    {
        SelectedSubject = null;
        (Topic, Message, IsContactOpen, _resumeRequest) = (string.Empty, string.Empty, false, false);
        ClearFilters();
    }

    // Cualquier cambio de filtro reaplica sobre la lista del paso visible.
    partial void OnQueryChanged(string value) => ApplyFilters();
    partial void OnProgramChanged(string value) => ApplyFilters();
    partial void OnOnlyWithAdvisorsChanged(bool value) => ApplyFilters();
    partial void OnKindChanged(string value) => ApplyFilters();
    partial void OnModalityChanged(string value) => ApplyFilters();
    partial void OnTimeBandChanged(Option<TimeBand> value) => ApplyFilters();

    // Al elegir materia se descarta el asesor previo y se cargan los de la nueva materia.
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

    // Al elegir asesor se descarta el horario previo y se cargan los del nuevo asesor.
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

    // Propone la próxima fecha del bloque para que el calendario abra en un día válido.
    partial void OnSelectedSlotChanged(AvailabilityItem? value)
        => SelectedDate = value?.NextOccurrence(DateTime.Today).Date;

    // Restablece todos los filtros a sus valores por omisión.
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

    // Retrocede un paso quitando la selección más reciente.
    [RelayCommand]
    private void Back()
    {
        if (SelectedAdvisor is not null) SelectedAdvisor = null;
        else SelectedSubject = null;
    }

    // Envía la solicitud. Sin sesión, pide entrar y la reanuda al volver; con un rol
    // que no es alumno, explica cómo cambiar de rol en vez de fallar en la API.
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

        // Fecha elegida + hora de inicio del bloque, en hora local del campus como espera la API.
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

    // Abre o cierra el formulario de mensaje; escribir exige sesión.
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

    // Envía el mensaje al asesor por correo y cierra el formulario si salió bien.
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

    // Aplica los filtros a la lista del paso visible. Solo esa: vaciar la lista
    // de un paso anterior haría que su ListBox borrara la selección (la materia
    // o el asesor elegidos) y la búsqueda regresaría sola al paso 1.
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
                // Al vaciarse la lista, el ListBox borra el horario elegido y con él la
                // fecha. Se guardan antes y se restauran si el bloque sigue en la lista:
                // así la solicitud pendiente sobrevive al regreso del login.
                var (slot, date) = (SelectedSlot, SelectedDate);
                Replace(Slots, _allSlots.Where(s =>
                    CatalogFilters.Matches(Modality, s.Modality) &&
                    CatalogFilters.Matches(TimeBand.Value, s.StartHour) &&
                    CatalogFilters.Matches(days, [s.DayOfWeek])));
                if (slot is not null && Slots.Contains(slot))
                {
                    // El orden importa: asignar el horario propone una fecha y luego se repone la elegida.
                    (SelectedSlot, SelectedDate) = (slot, date);
                }

                break;
        }

        OnPropertyChanged(nameof(ResultText));
        OnPropertyChanged(nameof(EmptyText));
        OnPropertyChanged(nameof(IsEmpty));
    }

    // Carga una lista del paso actual. La respuesta se descarta si la selección
    // ya cambió (stillCurrent), para no mezclar asesores de
    // una materia con los de otra.
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

    // Sustituye el contenido de la colección observable conservando la misma instancia enlazada.
    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items) target.Add(item);
    }

    // Tras cambiar de paso, refresca la lista y todas las propiedades que dependen del paso.
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
