using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Models;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

// Materia con su casilla de asignación para el asesor seleccionado.
public partial class AssignableSubject : ObservableObject
{
    // Inicializa el campo de respaldo directamente para no disparar notificaciones al construir.
    public AssignableSubject(SubjectItem subject, bool assigned) => (Id, Name, _isAssigned) = (subject.Id, subject.Name, assigned);

    public int Id { get; }
    public string Name { get; }

    [ObservableProperty] private bool _isAssigned;
}

// Dirección asigna qué materias cubre cada asesor en el ciclo (BPMN 2).
public partial class AdminViewModel : PageViewModel, IResettable
{
    private readonly IFcqiApi _api;
    private readonly ICatalogApi _catalog;
    // Listas completas en memoria; las colecciones observables solo guardan lo filtrado.
    private List<SubjectItem> _subjects = [];
    private List<AdvisorItem> _allAdvisors = [];
    private List<AssignableSubject> _assignable = [];

    [ObservableProperty] private AdvisorItem? _selectedAdvisor;
    [ObservableProperty] private string _query = string.Empty;
    [ObservableProperty] private string _kind = CatalogFilters.All;
    [ObservableProperty] private string _subjectQuery = string.Empty;

    // Recibe la API autenticada y el catálogo (para la lista de materias).
    public AdminViewModel(AppStatus status, IFcqiApi api, ICatalogApi catalog) : base(status)
    {
        _api = api;
        _catalog = catalog;
    }

    // Datos expuestos a la vista.
    public override string Title => "Dirección";
    public ObservableCollection<AdvisorItem> Advisors { get; } = [];
    public ObservableCollection<AssignableSubject> Subjects { get; } = [];
    public IReadOnlyList<string> Kinds => CatalogFilters.Kinds;
    public string ResultText => $"{Advisors.Count} de {_allAdvisors.Count} asesores";

    // Vacía la lista de asesores al cerrar sesión.
    public void Reset()
    {
        (_allAdvisors, SelectedAdvisor) = ([], null);
        Advisors.Clear();
    }

    // Métodos parciales que genera ObservableProperty: cada filtro se reaplica al teclear o elegir.
    partial void OnQueryChanged(string value) => FilterAdvisors();
    partial void OnKindChanged(string value) => FilterAdvisors();
    partial void OnSubjectQueryChanged(string value) => FilterSubjects();

    // Reaplica búsqueda y tipo sobre la lista completa. Conserva la selección
    // si sigue visible; si no, selecciona el primero para no dejar el panel vacío.
    private void FilterAdvisors()
    {
        var keep = SelectedAdvisor;
        Advisors.Clear();
        foreach (var a in _allAdvisors.Where(a => CatalogFilters.Contains(Query, a.FullName, a.Email) && CatalogFilters.Matches(Kind, a.Kind)))
        {
            Advisors.Add(a);
        }

        SelectedAdvisor = Advisors.Contains(keep!) ? keep : Advisors.FirstOrDefault();
        OnPropertyChanged(nameof(ResultText));
    }

    // Recarga al entrar para reflejar cambios hechos desde otra sesión.
    public override Task OnNavigatedToAsync() => RunAsync(ReloadAsync);

    // Arma las casillas de materias marcando las que ya tiene el asesor elegido.
    partial void OnSelectedAdvisorChanged(AdvisorItem? value)
    {
        var assigned = value?.Subjects.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        _assignable = value is null ? [] : _subjects.Select(s => new AssignableSubject(s, assigned.Contains(s.Name))).ToList();
        FilterSubjects();
    }

    // Filtra solo lo que se ve: las casillas marcadas y ocultas se conservan al guardar.
    private void FilterSubjects()
    {
        Subjects.Clear();
        foreach (var s in _assignable.Where(s => s.IsAssigned || CatalogFilters.Contains(SubjectQuery, s.Name)))
        {
            Subjects.Add(s);
        }
    }

    // Guarda el conjunto completo de materias marcadas (incluidas las ocultas
    // por el filtro) y recarga para mostrar el estado real del servidor.
    [RelayCommand]
    private Task SaveAsync()
    {
        if (SelectedAdvisor is not { } advisor) return Task.CompletedTask;

        var ids = _assignable.Where(s => s.IsAssigned).Select(s => s.Id).ToList();
        return RunAsync(async () =>
        {
            await _api.SaveAdvisorSubjectsAsync(advisor.Id, ids);
            await ReloadAsync();
        }, $"Materias de {advisor.FullName} guardadas. El catálogo ya muestra la nueva asignación.");
    }

    // Trae materias y asesores y restaura la selección previa por Id.
    private async Task ReloadAsync()
    {
        _subjects = await _catalog.GetSubjectsAsync();
        var keep = SelectedAdvisor?.Id;
        _allAdvisors = await _api.GetAdminAdvisorsAsync();
        FilterAdvisors();
        SelectedAdvisor = Advisors.FirstOrDefault(a => a.Id == keep) ?? Advisors.FirstOrDefault();
    }
}
