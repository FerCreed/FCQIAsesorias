using System.Collections.ObjectModel;
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Models;
using FCQI.Web.Serialization;

namespace FCQI.Web.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public const string ApiSubjectsUrl = "http://localhost:5016/api/subjects";

    [ObservableProperty]
    private string _status = "Presiona el botón para consultar la API.";

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<SubjectItem> Subjects { get; } = [];

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private async Task LoadSubjectsAsync()
    {
        IsBusy = true;
        Status = "Consultando GET /api/subjects…";
        Subjects.Clear();

        try
        {
            using var response = await _httpClient.GetAsync(ApiSubjectsUrl);
            response.EnsureSuccessStatusCode();

            var items = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.ListSubjectItem)
                        ?? [];

            foreach (var item in items)
            {
                Subjects.Add(item);
            }

            Status = $"HTTP {(int)response.StatusCode} OK — {Subjects.Count} materias desde la API.";
        }
        catch (Exception ex)
        {
            Status = $"No se pudo cargar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanLoad() => !IsBusy;

    partial void OnIsBusyChanged(bool value) => LoadSubjectsCommand.NotifyCanExecuteChanged();
}
