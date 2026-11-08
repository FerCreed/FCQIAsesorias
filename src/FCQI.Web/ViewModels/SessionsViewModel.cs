using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Models;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

/// <summary>
/// "Mis asesorías" del alumno y bandeja del asesor: próximas (BPMN 19) e
/// historial (BPMN 14). Desde aquí se confirma, rechaza, cancela, reprograma,
/// registra bitácora y se agrega la cita al calendario.
/// </summary>
public partial class SessionsViewModel : PageViewModel
{
    private readonly IFcqiApi _api;
    private readonly ICatalogApi _catalog;
    private readonly IPlatformServices _platform;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScopeTitle))]
    private bool _showHistory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection), nameof(CanConfirm), nameof(CanCancel), nameof(CanReschedule), nameof(CanLog))]
    private SessionItem? _selected;

    [ObservableProperty] private bool _isRescheduling;
    [ObservableProperty] private AvailabilityItem? _rescheduleSlot;
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

    public override string Title => Session.IsAdvisor ? "Solicitudes" : "Mis asesorías";
    public SessionContext Session { get; }
    public ObservableCollection<SessionItem> Sessions { get; } = [];
    public ObservableCollection<AvailabilityItem> RescheduleSlots { get; } = [];

    public string ScopeTitle => ShowHistory ? "Historial" : "Próximas";
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

    partial void OnShowHistoryChanged(bool value) => _ = RefreshAsync();

    partial void OnSelectedChanged(SessionItem? value)
    {
        IsRescheduling = false;
        IsLogging = false;
    }

    [RelayCommand]
    private void ShowScope(string history) => ShowHistory = history == "history";

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Selected = null;
        await RunAsync(async () =>
        {
            Sessions.Clear();
            foreach (var s in await _api.GetSessionsAsync(ShowHistory ? "history" : "upcoming"))
            {
                Sessions.Add(s);
            }
        });
        OnPropertyChanged(nameof(IsEmpty));
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
        if (Selected is null || RescheduleSlot is null)
        {
            Status.Error("Elige el nuevo horario.");
            return;
        }

        var body = new RescheduleBody(RescheduleSlot.Id, RescheduleSlot.NextOccurrence(DateTime.Today, Selected.ScheduledAt));
        if (await RunAsync(() => _api.RescheduleAsync(Selected.Id, body), "Asesoría reprogramada. El tutor debe confirmar el nuevo horario."))
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
    private async Task AddToCalendarAsync()
    {
        if (Selected is null) return;

        var id = Selected.Id;
        await RunAsync(async () =>
        {
            var ics = await _api.GetCalendarAsync(id);
            if (await _platform.SaveTextAsync($"asesoria-{id}.ics", ics))
            {
                Status.Info("Archivo guardado. Ábrelo para agregar la cita a tu calendario.");
            }
        });
    }

    [RelayCommand]
    private Task OpenMeetingAsync() => Selected?.MeetingLink is { } link ? _platform.OpenUrlAsync(link) : Task.CompletedTask;

    private async Task ChangeStatusAsync(string status, string done)
    {
        if (Selected is null) return;

        if (await RunAsync(() => _api.UpdateStatusAsync(Selected.Id, status), done))
        {
            await RefreshAsync();
        }
    }
}
