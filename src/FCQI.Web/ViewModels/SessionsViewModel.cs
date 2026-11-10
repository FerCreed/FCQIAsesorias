using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Models;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

/// <summary>
/// "Mis asesorías" del alumno y bandeja del asesor: próximas (BPMN 19) e
/// historial (BPMN 14), con calendario y filtros. Desde aquí se confirma,
/// rechaza, cancela, reprograma, registra bitácora, se agrega la cita al
/// calendario y se descarga su reporte individual.
/// </summary>
public partial class SessionsViewModel : PageViewModel, IResettable
{
    private readonly IFcqiApi _api;
    private readonly ICatalogApi _catalog;
    private readonly IPlatformServices _platform;
    private List<SessionItem> _all = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScopeTitle))]
    private bool _showHistory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection), nameof(CanConfirm), nameof(CanCancel), nameof(CanReschedule), nameof(CanLog))]
    private SessionItem? _selected;

    [ObservableProperty] private DateTime? _day;
    [ObservableProperty] private string _statusFilter = CatalogFilters.All;

    [ObservableProperty] private bool _isRescheduling;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RescheduleDates))]
    private AvailabilityItem? _rescheduleSlot;

    [ObservableProperty] private DateTime? _rescheduleDate;
    [ObservableProperty] private bool _isLogging;
    [ObservableProperty] private bool _attended = true;
    [ObservableProperty] private string _notes = string.Empty;

    public SessionsViewModel(AppStatus status, IFcqiApi api, ICatalogApi catalog, IPlatformServices platform, SessionContext session)
        : base(status)
    {
        _api = api;
        _catalog = catalog;
        _platform = platform;
        Session = session;
    }

    /// <summary>Límites del calendario de reprogramación.</summary>
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

    /// <summary>Días con asesorías: el calendario solo deja elegir esos.</summary>
    public IReadOnlyList<DateTime> SessionDays => _all.Select(s => s.ScheduledAt.Date).Distinct().ToList();
    public DateTime CalendarStart => SessionDays.Count == 0 ? DateTime.Today : SessionDays.Min();
    public DateTime CalendarEnd => SessionDays.Count == 0 ? DateTime.Today : SessionDays.Max();

    public IReadOnlyList<DateTime>? RescheduleDates => RescheduleSlot is null
        ? null
        : CatalogFilters.DatesOn(RescheduleSlot.DayOfWeek, Tomorrow, Horizon).ToList();

    public string ScopeTitle => ShowHistory ? "Historial" : "Próximas";
    public string ResultText => $"{Sessions.Count} de {_all.Count} asesorías";
    public bool IsEmpty => Sessions.Count == 0 && !IsBusy;
    public bool HasSelection => Selected is not null;
    public bool CanConfirm => Session.IsAdvisor && Selected?.IsPending == true;
    public bool CanCancel => Selected?.IsOpen == true;
    public bool CanReschedule => Session.IsStudent && Selected?.IsOpen == true;
    public bool CanLog => Session.IsAdvisor && Selected?.IsConfirmed == true && Selected.ScheduledAt <= DateTime.Now;

    public override Task OnNavigatedToAsync()
    {
        OnPropertyChanged(nameof(Title));
        return RefreshAsync();
    }

    public void Reset()
    {
        (_all, Selected, Day, StatusFilter, ShowHistory) = ([], null, null, CatalogFilters.All, false);
        Sessions.Clear();
        RescheduleSlots.Clear();
    }

    partial void OnShowHistoryChanged(bool value) => _ = RefreshAsync();
    partial void OnDayChanged(DateTime? value) => ApplyFilters();
    partial void OnStatusFilterChanged(string value) => ApplyFilters();

    partial void OnSelectedChanged(SessionItem? value)
    {
        IsRescheduling = false;
        IsLogging = false;
    }

    partial void OnRescheduleSlotChanged(AvailabilityItem? value)
        => RescheduleDate = value?.NextOccurrence(DateTime.Today, Selected?.ScheduledAt).Date;

    [RelayCommand]
    private void ShowScope(string history) => ShowHistory = history == "history";

    [RelayCommand]
    private void ClearFilters() => (Day, StatusFilter) = (null, CatalogFilters.All);

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

    [RelayCommand] private Task ConfirmAsync() => ChangeStatusAsync(SessionStatuses.Confirmed, "Asesoría confirmada. Avisamos al alumno.");
    [RelayCommand] private Task RejectAsync() => ChangeStatusAsync(SessionStatuses.Rejected, "Solicitud rechazada. Avisamos al alumno.");
    [RelayCommand] private Task CancelAsync() => ChangeStatusAsync(SessionStatuses.Cancelled, "Asesoría cancelada. El lugar quedó libre.");

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

    [RelayCommand]
    private async Task RescheduleAsync()
    {
        if (Selected is null || RescheduleSlot is null || RescheduleDate is null)
        {
            Status.Error("Elige el nuevo horario y la fecha en el calendario.");
            return;
        }

        var start = RescheduleDate.Value.Date + (TimeSpan.TryParse(RescheduleSlot.StartTime, out var t) ? t : TimeSpan.Zero);
        if (await RunAsync(() => _api.RescheduleAsync(Selected.Id, new RescheduleBody(RescheduleSlot.Id, start)),
                $"Asesoría reprogramada al {start:dd/MM/yyyy HH:mm}. El asesor debe confirmar el nuevo horario."))
        {
            await RefreshAsync();
        }
    }

    [RelayCommand]
    private void StartLog()
    {
        IsRescheduling = false;
        (Attended, Notes, IsLogging) = (true, string.Empty, true);
    }

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

    [RelayCommand]
    private Task AddToCalendarAsync() => DownloadAsync(
        id => _api.GetCalendarAsync(id), "asesoria-{0}.ics", "text/calendar",
        "Archivo de calendario descargado. Ábrelo para agregar la cita a tu calendario.");

    /// <summary>Reporte individual (PDF): lo descargan el alumno, el asesor y dirección.</summary>
    [RelayCommand]
    private Task DownloadReportAsync() => DownloadAsync(
        id => _api.GetSessionReportAsync(id), "asesoria-{0}.pdf", "application/pdf", "Reporte de la asesoría descargado.");

    [RelayCommand]
    private Task OpenMeetingAsync() => Selected?.MeetingLink is { } link ? _platform.OpenUrlAsync(link) : Task.CompletedTask;

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

    private async Task ChangeStatusAsync(string status, string done)
    {
        if (Selected is null) return;

        if (await RunAsync(() => _api.UpdateStatusAsync(Selected.Id, status), done))
        {
            await RefreshAsync();
        }
    }

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
