using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Models;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

/// <summary>Métricas del ciclo y constancia de horas por asesor (BPMN 6).</summary>
public partial class ReportsViewModel : PageViewModel
{
    private readonly IFcqiApi _api;
    private readonly IPlatformServices _platform;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasData))]
    private ReportItem? _report;

    public ReportsViewModel(AppStatus status, IFcqiApi api, IPlatformServices platform) : base(status)
    {
        _api = api;
        _platform = platform;
    }

    public override string Title => "Reportes";
    public bool HasData => Report?.HasData == true;

    public override Task OnNavigatedToAsync() => RunAsync(async () => Report = await _api.GetReportAsync());

    [RelayCommand]
    private Task ExportAsync()
        => RunAsync(async () =>
        {
            var csv = await _api.GetReportCsvAsync();
            if (await _platform.SaveTextAsync($"constancia-horas-{Report?.Term}.csv", csv))
            {
                Status.Info("Constancia guardada. Se abre en Excel u Hojas de cálculo.");
            }
        });
}
