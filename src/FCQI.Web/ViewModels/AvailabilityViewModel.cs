using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Models;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

/// <summary>Horarios del asesor: configurar (BPMN 7), editar (BPMN 13) y retirar (BPMN 16).</summary>
public partial class AvailabilityViewModel : PageViewModel
{
    private readonly IFcqiApi _api;
    private readonly ICatalogApi _catalog;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormTitle), nameof(IsEditing))]
    private int? _editingId;

    [ObservableProperty] private DayOption? _day;
    [ObservableProperty] private string _start = "12:00";
    [ObservableProperty] private string _end = "13:00";
    [ObservableProperty] private decimal? _capacity = 1;
    [ObservableProperty] private LocationItem? _location;

    public AvailabilityViewModel(AppStatus status, IFcqiApi api, ICatalogApi catalog) : base(status)
    {
        _api = api;
        _catalog = catalog;
        _day = Days[0];
    }

    public override string Title => "Mis horarios";
    public IReadOnlyList<DayOption> Days => DayNames.Weekdays;
    public ObservableCollection<AvailabilityItem> Blocks { get; } = [];
    public ObservableCollection<LocationItem> Locations { get; } = [];
    public bool IsEditing => EditingId is not null;
    public string FormTitle => IsEditing ? "Editar bloque" : "Nuevo bloque";
    public bool IsEmpty => Blocks.Count == 0 && !IsBusy;

    public override async Task OnNavigatedToAsync()
    {
        await RunAsync(async () =>
        {
            if (Locations.Count == 0)
            {
                foreach (var l in await _catalog.GetLocationsAsync()) Locations.Add(l);
                Location ??= Locations.FirstOrDefault();
            }

            await ReloadAsync();
        });
    }

    [RelayCommand]
    private void New()
    {
        EditingId = null;
        (Day, Start, End, Capacity) = (Days[0], "12:00", "13:00", 1);
        Location = Locations.FirstOrDefault();
    }

    [RelayCommand]
    private void Edit(AvailabilityItem block)
    {
        EditingId = block.Id;
        Day = Days.FirstOrDefault(d => d.Value == block.DayOfWeek) ?? Days[0];
        (Start, End, Capacity) = (block.StartTime[..5], block.EndTime[..5], block.MaxCapacity);
        Location = Locations.FirstOrDefault(l => l.Name == block.Location) ?? Locations.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var body = BuildBody(out var problem);
        if (body is null)
        {
            Status.Error(problem!);
            return;
        }

        var id = EditingId;
        if (await RunAsync(async () =>
            {
                if (id is null) await _api.CreateAvailabilityAsync(body);
                else await _api.UpdateAvailabilityAsync(id.Value, body);
                await ReloadAsync();
            }, id is null ? "Horario publicado. Los alumnos ya lo ven en el catálogo." : "Horario actualizado."))
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

    /// <summary>Validación previa del formulario; la API repite las reglas de negocio.</summary>
    private AvailabilityBody? BuildBody(out string? problem)
    {
        problem = null;
        var end = default(TimeOnly);
        if (!TimeOnly.TryParse(Start, out var start) || !TimeOnly.TryParse(End, out end))
        {
            problem = "Escribe las horas como 12:00.";
        }
        else if (end <= start)
        {
            problem = "La hora de fin debe ser posterior a la de inicio.";
        }
        else if (Day is null || Location is null || Capacity is null or < 1)
        {
            problem = "Completa día, lugar y cupo.";
        }

        return problem is null
            ? new AvailabilityBody(Day!.Value, start.ToString("HH:mm:ss"), end.ToString("HH:mm:ss"), (int)Capacity!.Value, Location!.Id)
            : null;
    }

    private async Task ReloadAsync()
    {
        Blocks.Clear();
        foreach (var b in await _api.GetMyAvailabilitiesAsync()) Blocks.Add(b);
        OnPropertyChanged(nameof(IsEmpty));
    }
}
