using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Models;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

/// <summary>
/// Horarios del asesor: configurar (BPMN 7), editar (BPMN 13) y retirar
/// (BPMN 16). También descarga su resumen de asesorías y horas trabajadas.
/// </summary>
public partial class AvailabilityViewModel : PageViewModel, IResettable
{
    private readonly IFcqiApi _api;
    private readonly ICatalogApi _catalog;
    private readonly IPlatformServices _platform;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormTitle), nameof(IsEditing))]
    private int? _editingId;

    [ObservableProperty] private DayOption? _day;
    [ObservableProperty] private TimeSpan? _start = new(12, 0, 0);
    [ObservableProperty] private TimeSpan? _end = new(13, 0, 0);
    [ObservableProperty] private decimal? _capacity = 1;
    [ObservableProperty] private LocationItem? _location;

    // Resumen del asesor
    [ObservableProperty] private DateTime? _summaryFrom;
    [ObservableProperty] private DateTime? _summaryTo = DateTime.Today;

    public AvailabilityViewModel(AppStatus status, IFcqiApi api, ICatalogApi catalog, IPlatformServices platform) : base(status)
    {
        _api = api;
        _catalog = catalog;
        _platform = platform;
        _day = Days[0];
    }

    public override string Title => "Mis horarios";
    public IReadOnlyList<DayOption> Days => DayNames.Weekdays;
    public ObservableCollection<AvailabilityItem> Blocks { get; } = [];
    public ObservableCollection<LocationItem> Locations { get; } = [];
    public bool IsEditing => EditingId is not null;
    public string FormTitle => IsEditing ? "Editar bloque" : "Nuevo bloque";
    public bool IsEmpty => Blocks.Count == 0 && !IsBusy;
    public double WeeklyHours => Math.Round(Blocks.Sum(b => (TimeSpan.Parse(b.EndTime) - TimeSpan.Parse(b.StartTime)).TotalHours), 1);

    public override async Task OnNavigatedToAsync()
    {
        await RunAsync(async () =>
        {
            if (Locations.Count == 0)
            {
                foreach (var l in await _catalog.GetLocationsAsync()) Locations.Add(l);
                Location ??= Locations.FirstOrDefault();
                SummaryFrom ??= (await _catalog.GetTermAsync()).StartsOn.ToDateTime(TimeOnly.MinValue);
            }

            await ReloadAsync();
        });
    }

    public void Reset()
    {
        Blocks.Clear();
        New();
    }

    [RelayCommand]
    private void New()
    {
        EditingId = null;
        (Day, Start, End, Capacity) = (Days[0], new TimeSpan(12, 0, 0), new TimeSpan(13, 0, 0), 1);
        Location = Locations.FirstOrDefault();
    }

    [RelayCommand]
    private void Edit(AvailabilityItem block)
    {
        EditingId = block.Id;
        Day = Days.FirstOrDefault(d => d.Value == block.DayOfWeek) ?? Days[0];
        (Start, End, Capacity) = (TimeSpan.Parse(block.StartTime), TimeSpan.Parse(block.EndTime), block.MaxCapacity);
        Location = Locations.FirstOrDefault(l => l.Name == block.Location) ?? Locations.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Validate() is { } problem)
        {
            Status.Error(problem);
            return;
        }

        var body = new AvailabilityBody(Day!.Value, $"{Start:hh\\:mm}:00", $"{End:hh\\:mm}:00", (int)Capacity!.Value, Location!.Id);
        var id = EditingId;
        if (await RunAsync(async () =>
            {
                if (id is null) await _api.CreateAvailabilityAsync(body);
                else await _api.UpdateAvailabilityAsync(id.Value, body);
                await ReloadAsync();
            }, id is null ? "Horario publicado. Los alumnos ya lo ven en el buscador." : "Horario actualizado."))
        {
            New();
        }
    }

    [RelayCommand]
    private Task RetireAsync(AvailabilityItem block)
        => RunAsync(async () =>
        {
            await _api.RetireAvailabilityAsync(block.Id);
            await ReloadAsync();
        }, $"Bloque del {block.DayName.ToLowerInvariant()} {block.TimeRange} retirado.");

    /// <summary>Resumen de asesorías y horas trabajadas del periodo (PDF o CSV).</summary>
    [RelayCommand]
    private async Task DownloadSummaryAsync(string format)
    {
        if (SummaryFrom is null || SummaryTo is null)
        {
            Status.Error("Elige en el calendario el periodo del resumen.");
            return;
        }

        var (from, to) = (DateOnly.FromDateTime(SummaryFrom.Value), DateOnly.FromDateTime(SummaryTo.Value));
        await RunAsync(async () =>
        {
            var bytes = await _api.GetMyReportAsync(format, from, to);
            if (await _platform.SaveFileAsync($"resumen-asesorias-{from:yyyyMMdd}-{to:yyyyMMdd}.{format}",
                    format == "pdf" ? "application/pdf" : "text/csv", bytes))
            {
                Status.Info("Resumen descargado.");
            }
        });
    }

    /// <summary>Validación previa del formulario; la API repite las reglas de negocio.</summary>
    private string? Validate()
    {
        if (Start is null || End is null) return "Elige la hora de inicio y la de fin.";
        if (End <= Start) return "La hora de fin debe ser posterior a la de inicio.";
        if (Day is null || Location is null || Capacity is null or < 1) return "Completa día, lugar y cupo.";
        return null;
    }

    private async Task ReloadAsync()
    {
        Blocks.Clear();
        foreach (var b in await _api.GetMyAvailabilitiesAsync()) Blocks.Add(b);
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(WeeklyHours));
    }
}
