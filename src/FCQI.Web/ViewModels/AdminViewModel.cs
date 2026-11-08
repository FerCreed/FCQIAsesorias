using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Models;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

public partial class AssignableSubject : ObservableObject
{
    public AssignableSubject(SubjectItem subject, bool assigned) => (Id, Name, _isAssigned) = (subject.Id, subject.Name, assigned);

    public int Id { get; }
    public string Name { get; }

    [ObservableProperty] private bool _isAssigned;
}

/// <summary>Dirección asigna qué materias cubre cada asesor en el ciclo (BPMN 2).</summary>
public partial class AdminViewModel : PageViewModel, IResettable
{
    private readonly IFcqiApi _api;
    private readonly ICatalogApi _catalog;
    private List<SubjectItem> _subjects = [];
    private List<AdvisorItem> _allAdvisors = [];
    private List<AssignableSubject> _assignable = [];

    [ObservableProperty] private AdvisorItem? _selectedAdvisor;
    [ObservableProperty] private string _query = string.Empty;
    [ObservableProperty] private string _kind = CatalogFilters.All;
    [ObservableProperty] private string _subjectQuery = string.Empty;

    public AdminViewModel(AppStatus status, IFcqiApi api, ICatalogApi catalog) : base(status)
    {
        _api = api;
        _catalog = catalog;
    }

    public override string Title => "Dirección";
    public ObservableCollection<AdvisorItem> Advisors { get; } = [];
    public ObservableCollection<AssignableSubject> Subjects { get; } = [];
    public IReadOnlyList<string> Kinds => CatalogFilters.Kinds;
    public string ResultText => $"{Advisors.Count} de {_allAdvisors.Count} asesores";

    public void Reset()
    {
        (_allAdvisors, SelectedAdvisor) = ([], null);
        Advisors.Clear();
    }

    partial void OnQueryChanged(string value) => FilterAdvisors();
    partial void OnKindChanged(string value) => FilterAdvisors();
    partial void OnSubjectQueryChanged(string value) => FilterSubjects();

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

    public override Task OnNavigatedToAsync() => RunAsync(ReloadAsync);

    partial void OnSelectedAdvisorChanged(AdvisorItem? value)
    {
        var assigned = value?.Subjects.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        _assignable = value is null ? [] : _subjects.Select(s => new AssignableSubject(s, assigned.Contains(s.Name))).ToList();
        FilterSubjects();
    }

    /// <summary>Filtra solo lo que se ve: las casillas marcadas y ocultas se conservan al guardar.</summary>
    private void FilterSubjects()
    {
        Subjects.Clear();
        foreach (var s in _assignable.Where(s => s.IsAssigned || CatalogFilters.Contains(SubjectQuery, s.Name)))
        {
            Subjects.Add(s);
        }
    }

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

    private async Task ReloadAsync()
    {
        _subjects = await _catalog.GetSubjectsAsync();
        var keep = SelectedAdvisor?.Id;
        _allAdvisors = await _api.GetAdminAdvisorsAsync();
        FilterAdvisors();
        SelectedAdvisor = Advisors.FirstOrDefault(a => a.Id == keep) ?? Advisors.FirstOrDefault();
    }
}
