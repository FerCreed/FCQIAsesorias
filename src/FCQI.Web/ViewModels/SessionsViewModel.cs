using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Models;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

// "Mis asesorías" del alumno y bandeja del asesor: próximas (BPMN 19) e
// historial (BPMN 14), con calendario y filtros. Desde aquí se confirma,
// rechaza, cancela, reprograma, registra bitácora, se agrega la cita al
// calendario y se descarga su reporte individual.
public partial class SessionsViewModel : PageViewModel, IResettable
{
    private readonly IFcqiApi _api;
    private readonly ICatalogApi _catalog;
    private readonly IPlatformServices _platform;
    // Lista completa del alcance actual; Sessions guarda solo lo filtrado.
    private List<SessionItem> _all = [];

    // Alterna entre próximas e historial.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScopeTitle))]
    private bool _showHistory;

    // Asesoría seleccionada; de ella dependen los botones de acción habilitados.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection), nameof(CanConfirm), nameof(CanCancel), nameof(CanReschedule), nameof(CanLog))]
    private SessionItem? _selected;

    // Filtros de la lista: día del calendario y estado.
    [ObservableProperty] private DateTime? _day;
    [ObservableProperty] private string _statusFilter = CatalogFilters.All;

    // Estado del panel de reprogramación.
    [ObservableProperty] private bool _isRescheduling;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RescheduleDates))]
    private AvailabilityItem? _rescheduleSlot;

    [ObservableProperty] private DateTime? _rescheduleDate;
    // Estado del panel de bitácora; asistencia marcada por omisión, que es lo común.
    [ObservableProperty] private bool _isLogging;
    [ObservableProperty] private bool _attended = true;
    [ObservableProperty] private string _notes = string.Empty;

    // Recibe la API, el catálogo (horarios para reprogramar), la plataforma (descargas) y la sesión (rol).
    public SessionsViewModel(AppStatus status, IFcqiApi api, ICatalogApi catalog, IPlatformServices platform, SessionContext session)
        : base(status)
    {
        _api = api;
        _catalog = catalog;
        _platform = platform;
        Session = session;
    }

    // Límites del calendario de reprogramación.
    public static DateTime Tomorrow => DateTime.Today.AddDays(1);
    public static DateTime Horizon => DateTime.Today.AddMonths(4);

    public override string Title => Session.IsAdvisor ? "Solicitudes" : "Mis asesorías";
    public SessionContext Session { get; }
    public ObservableCollection<SessionItem> Sessions { get; } = [];
    public ObservableCollection<AvailabilityItem> RescheduleSlots { get; } = [];

    public IReadOnlyList<string> Statuses { get; } =
    [
        CatalogFilters.All, SessionStatuses.Pending, SessionStatuses.Confirmed, SessionStatuses.Completed,
        SessionStatuses.Cancelled, SessionStatuses.Rejected
    ];

    // Días con asesorías: el calendario solo deja elegir esos.
    public IReadOnlyList<DateTime> SessionDays => _all.Select(s => s.ScheduledAt.Date).Distinct().ToList();
    public DateTime CalendarStart => SessionDays.Count == 0 ? DateTime.Today : SessionDays.Min();
    public DateTime CalendarEnd => SessionDays.Count == 0 ? DateTime.Today : SessionDays.Max();

    // Fechas válidas para el bloque elegido: solo los días de la semana en que se imparte.
    public IReadOnlyList<DateTime>? RescheduleDates => RescheduleSlot is null
        ? null
        : CatalogFilters.DatesOn(RescheduleSlot.DayOfWeek, Tomorrow, Horizon).ToList();

    public string ScopeTitle => ShowHistory ? "Historial" : "Próximas";
    public string ResultText => $"{Sessions.Count} de {_all.Count} asesorías";
    public bool IsEmpty => Sessions.Count == 0 && !IsBusy;
    public bool HasSelection => Selected is not null;
    // Reglas de botones: reflejan las de la API (SessionState) para no ofrecer acciones que fallarían.
    public bool CanConfirm => Session.IsAdvisor && Selected?.IsPending == true;
    public bool CanCancel => Selected?.IsOpen == true;
    public bool CanReschedule => Session.IsStudent && Selected?.IsOpen == true;
    public bool CanLog => Session.IsAdvisor && Selected?.IsConfirmed == true && Selected.ScheduledAt <= DateTime.Now;

    // El título depende del rol activo, que pudo cambiar desde la última visita.
    public override Task OnNavigatedToAsync()
    {
        OnPropertyChanged(nameof(Title));
        return RefreshAsync();
    }

    // Al cerrar sesión se vacían listas, selección y filtros.
    public void Reset()
    {
        (_all, Selected, Day, StatusFilter, ShowHistory) = ([], null, null, CatalogFilters.All, false);
        Sessions.Clear();
        RescheduleSlots.Clear();
    }

    // Cambiar de alcance pide otra lista al servidor; los filtros solo reaplican en memoria.
    partial void OnShowHistoryChanged(bool value) => _ = RefreshAsync();
    partial void OnDayChanged(DateTime? value) => ApplyFilters();
    partial void OnStatusFilterChanged(string value) => ApplyFilters();

    // Al elegir otra asesoría se cierran los paneles abiertos de la anterior.
    partial void OnSelectedChanged(SessionItem? value)
    {
        IsRescheduling = false;
        IsLogging = false;
    }

    // Propone la próxima fecha del bloque, evitando la misma cita que se reprograma.
    partial void OnRescheduleSlotChanged(AvailabilityItem? value)
        => RescheduleDate = value?.NextOccurrence(DateTime.Today, Selected?.ScheduledAt).Date;

    // Pestañas Próximas/Historial: el parámetro llega como texto desde el XAML.
    [RelayCommand]
    private void ShowScope(string history) => ShowHistory = history == "history";

    // «Ver todas»: quita el día y el estado elegidos.
    [RelayCommand]
    private void ClearFilters() => (Day, StatusFilter) = (null, CatalogFilters.All);

    // Recarga del servidor y recalcula los días habilitados del calendario.
    [RelayCommand]
    private async Task RefreshAsync()
    {
        Selected = null;
        await RunAsync(async () => _all = await _api.GetSessionsAsync(ShowHistory ? "history" : "upcoming"));
        Day = null;
        OnPropertyChanged(nameof(SessionDays));
        OnPropertyChanged(nameof(CalendarStart));
        OnPropertyChanged(nameof(CalendarEnd));
        ApplyFilters();
    }

    // Cambios de estado: comparten ChangeStatusAsync y solo varían destino y mensaje.
    [RelayCommand] private Task ConfirmAsync() => ChangeStatusAsync(SessionStatuses.Confirmed, "Asesoría confirmada. Avisamos al alumno.");
    [RelayCommand] private Task RejectAsync() => ChangeStatusAsync(SessionStatuses.Rejected, "Solicitud rechazada. Avisamos al alumno.");
    [RelayCommand] private Task CancelAsync() => ChangeStatusAsync(SessionStatuses.Cancelled, "Asesoría cancelada. El lugar quedó libre.");

    // Abre el panel de reprogramación con los horarios actuales del mismo asesor.
    [RelayCommand]
    private async Task StartRescheduleAsync()
    {
        if (Selected is null) return;

        IsLogging = false;
        RescheduleSlots.Clear();
        var advisorId = Selected.AdvisorId;
        if (await RunAsync(async () =>
            {
                foreach (var slot in await _catalog.GetAvailabilitiesAsync(advisorId))
                {
                    RescheduleSlots.Add(slot);
                }
            }))
        {
            IsRescheduling = true;
        }
    }

    // Envía la reprogramación; la API cancela la cita vieja y crea la nueva como Pendiente.
    [RelayCommand]
    private async Task RescheduleAsync()
    {
        if (Selected is null || RescheduleSlot is null || RescheduleDate is null)
        {
            Status.Error("Elige el nuevo horario y la fecha en el calendario.");
            return;
        }

        // Fecha elegida + hora de inicio del bloque = instante local que valida la API.
        var start = RescheduleDate.Value.Date + (TimeSpan.TryParse(RescheduleSlot.StartTime, out var t) ? t : TimeSpan.Zero);
        if (await RunAsync(() => _api.RescheduleAsync(Selected.Id, new RescheduleBody(RescheduleSlot.Id, start)),
                $"Asesoría reprogramada al {start:dd/MM/yyyy HH:mm}. El asesor debe confirmar el nuevo horario."))
        {
            await RefreshAsync();
        }
    }

    // Abre el panel de bitácora con valores limpios.
    [RelayCommand]
    private void StartLog()
    {
        IsRescheduling = false;
        (Attended, Notes, IsLogging) = (true, string.Empty, true);
    }

    // Guarda la bitácora; la API marca la asesoría como realizada.
    [RelayCommand]
    private async Task SaveLogAsync()
    {
        if (Selected is null) return;

        if (await RunAsync(() => _api.RecordLogAsync(Selected.Id, new SessionLogBody(Attended, Notes)),
                "Bitácora guardada. La asesoría quedó como realizada."))
        {
            await RefreshAsync();
        }
    }

    // Descarga el .ics de la cita para importarlo en cualquier calendario.
    [RelayCommand]
    private Task AddToCalendarAsync() => DownloadAsync(
        id => _api.GetCalendarAsync(id), "asesoria-{0}.ics", "text/calendar",
        "Archivo de calendario descargado. Ábrelo para agregar la cita a tu calendario.");

    // Reporte individual (PDF): lo descargan el alumno, el asesor y dirección.
    [RelayCommand]
    private Task DownloadReportAsync() => DownloadAsync(
        id => _api.GetSessionReportAsync(id), "asesoria-{0}.pdf", "application/pdf", "Reporte de la asesoría descargado.");

    // Abre el enlace de la reunión virtual en el navegador del sistema.
    [RelayCommand]
    private Task OpenMeetingAsync() => Selected?.MeetingLink is { } link ? _platform.OpenUrlAsync(link) : Task.CompletedTask;

    // Descarga común de archivos de la asesoría seleccionada; el Id se copia antes
    // de esperar porque la selección puede cambiar mientras llega la respuesta.
    private async Task DownloadAsync(Func<int, Task<byte[]>> fetch, string nameFormat, string mime, string done)
    {
        if (Selected is null) return;

        var id = Selected.Id;
        await RunAsync(async () =>
        {
            if (await _platform.SaveFileAsync(string.Format(nameFormat, id), mime, await fetch(id)))
            {
                Status.Info(done);
            }
        });
    }

    // Cambia el estado y recarga para reflejar el resultado real del servidor.
    private async Task ChangeStatusAsync(string status, string done)
    {
        if (Selected is null) return;

        if (await RunAsync(() => _api.UpdateStatusAsync(Selected.Id, status), done))
        {
            await RefreshAsync();
        }
    }

    // Reaplica día y estado sobre la lista en memoria, sin volver al servidor.
    private void ApplyFilters()
    {
        Sessions.Clear();
        foreach (var s in _all.Where(s =>
                     (Day is null || s.ScheduledAt.Date == Day.Value.Date) &&
                     CatalogFilters.Matches(StatusFilter, s.Status)))
        {
            Sessions.Add(s);
        }

        OnPropertyChanged(nameof(ResultText));
        OnPropertyChanged(nameof(IsEmpty));
    }
}
