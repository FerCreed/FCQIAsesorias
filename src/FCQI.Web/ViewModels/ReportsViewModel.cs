using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Models;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

/// <summary>
/// Reportes de dirección (BPMN 6): tablero de métricas de las asesorías
/// creadas en el periodo, descarga de todas las asesorías (CSV/PDF) y del
/// resumen de cualquier asesor. Las fechas se eligen en calendario.
/// </summary>
public partial class ReportsViewModel : PageViewModel, IResettable
{
    private static readonly AdvisorItem AllAdvisors = new() { Id = 0, FullName = "Todos los asesores" };
    private static readonly SubjectItem AllSubjects = new() { Id = 0, Name = "Todas las materias" };

    private readonly IFcqiApi _api;
    private readonly ICatalogApi _catalog;
    private readonly IPlatformServices _platform;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasData), nameof(Metrics))]
    private DashboardItem? _dashboard;

    [ObservableProperty] private DateTime? _from;
    [ObservableProperty] private DateTime? _to = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAdvisor))]
    private AdvisorItem? _advisor = AllAdvisors;

    [ObservableProperty] private SubjectItem? _subject = AllSubjects;
    [ObservableProperty] private string _statusFilter = CatalogFilters.All;

    public ReportsViewModel(AppStatus status, IFcqiApi api, ICatalogApi catalog, IPlatformServices platform) : base(status)
    {
        _api = api;
        _catalog = catalog;
        _platform = platform;
    }

    public override string Title => "Reportes";
    public ObservableCollection<AdvisorItem> Advisors { get; } = [AllAdvisors];
    public ObservableCollection<SubjectItem> Subjects { get; } = [AllSubjects];

    public IReadOnlyList<string> Statuses { get; } =
    [
        CatalogFilters.All, SessionStatuses.Pending, SessionStatuses.Confirmed, SessionStatuses.Completed,
        SessionStatuses.Cancelled, SessionStatuses.Rejected
    ];

    public MetricsItem? Metrics => Dashboard?.Metrics;
    public bool HasData => Dashboard?.Metrics.HasData == true;
    public bool HasAdvisor => Advisor is { Id: > 0 };

    public override async Task OnNavigatedToAsync()
    {
        await RunAsync(async () =>
        {
            if (Advisors.Count == 1)
            {
                From ??= (await _catalog.GetTermAsync()).StartsOn.ToDateTime(TimeOnly.MinValue);
                foreach (var a in await _api.GetAdminAdvisorsAsync()) Advisors.Add(a);
                foreach (var s in await _catalog.GetSubjectsAsync()) Subjects.Add(s);
            }
        });
        await RefreshAsync();
    }

    public void Reset() => Dashboard = null;

    [RelayCommand]
    private Task RefreshAsync()
        => Filter() is { } filter
            ? RunAsync(async () => Dashboard = await _api.GetDashboardAsync(filter))
            : Task.CompletedTask;

    /// <summary>Todas las asesorías creadas desde la fecha elegida, con todos sus datos.</summary>
    [RelayCommand]
    private Task ExportAsync(string format)
        => Filter() is { } filter
            ? SaveAsync($"asesorias-{filter.From:yyyyMMdd}-{filter.To:yyyyMMdd}.{format}", format,
                () => _api.GetSessionsExportAsync(format, filter))
            : Task.CompletedTask;

    /// <summary>Resumen del asesor elegido: sus asesorías y horas trabajadas.</summary>
    [RelayCommand]
    private Task ExportAdvisorAsync(string format)
    {
        if (!HasAdvisor)
        {
            Status.Error("Elige un asesor en los filtros para descargar su resumen.");
            return Task.CompletedTask;
        }

        return Filter() is { } filter
            ? SaveAsync($"resumen-asesor-{Advisor!.Id}-{filter.From:yyyyMMdd}-{filter.To:yyyyMMdd}.{format}", format,
                () => _api.GetAdvisorReportAsync(Advisor.Id, format, filter.From, filter.To))
            : Task.CompletedTask;
    }

    private ReportFilterItem? Filter()
    {
        if (From is null || To is null)
        {
            Status.Error("Elige en el calendario la fecha inicial y la final.");
            return null;
        }

        return new ReportFilterItem(
            DateOnly.FromDateTime(From.Value), DateOnly.FromDateTime(To.Value),
            HasAdvisor ? Advisor!.Id : null,
            Subject is { Id: > 0 } ? Subject.Id : null,
            StatusFilter == CatalogFilters.All ? null : StatusFilter);
    }

    private Task SaveAsync(string name, string format, Func<Task<byte[]>> fetch)
        => RunAsync(async () =>
        {
            if (await _platform.SaveFileAsync(name, format == "pdf" ? "application/pdf" : "text/csv", await fetch()))
            {
                Status.Info($"Reporte descargado: {name}.");
            }
        });
}
