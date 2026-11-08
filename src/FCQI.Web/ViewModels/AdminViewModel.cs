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
public partial class AdminViewModel : PageViewModel
{
    private readonly IFcqiApi _api;
    private readonly ICatalogApi _catalog;
    private List<SubjectItem> _subjects = [];

    [ObservableProperty] private AdvisorItem? _selectedAdvisor;

    public AdminViewModel(AppStatus status, IFcqiApi api, ICatalogApi catalog) : base(status)
    {
        _api = api;
        _catalog = catalog;
    }

    public override string Title => "Dirección";
    public ObservableCollection<AdvisorItem> Advisors { get; } = [];
    public ObservableCollection<AssignableSubject> Subjects { get; } = [];

    public override Task OnNavigatedToAsync() => RunAsync(ReloadAsync);

    partial void OnSelectedAdvisorChanged(AdvisorItem? value)
    {
        Subjects.Clear();
        if (value is null) return;

        var assigned = value.Subjects.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var s in _subjects)
        {
            Subjects.Add(new AssignableSubject(s, assigned.Contains(s.Name)));
        }
    }

    [RelayCommand]
    private Task SaveAsync()
    {
        if (SelectedAdvisor is not { } advisor) return Task.CompletedTask;

        var ids = Subjects.Where(s => s.IsAssigned).Select(s => s.Id).ToList();
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
        Advisors.Clear();
        foreach (var a in await _api.GetAdminAdvisorsAsync()) Advisors.Add(a);
        SelectedAdvisor = Advisors.FirstOrDefault(a => a.Id == keep) ?? Advisors.FirstOrDefault();
    }
}
