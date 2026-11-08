using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FCQI.Web.ViewModels;
using FCQI.Web.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FCQI.Web;

public partial class App : Application
{
    /// <summary>Dirección de la API. La fija cada anfitrión (navegador o escritorio) antes de arrancar.</summary>
    public static Uri ApiBaseAddress { get; set; } = new("http://localhost:8080");

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var view = new ShellView();
        var services = AppServices.Build(ApiBaseAddress, () => TopLevel.GetTopLevel(view));
        var shell = services.GetRequiredService<ShellViewModel>();
        view.DataContext = shell;

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new Window
                {
                    Title = "Asesorías FCQI",
                    Content = view,
                    Width = 1120,
                    Height = 780,
                    MinWidth = 360,
                    MinHeight = 560
                };
                break;
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = view;
                break;
        }

        _ = shell.StartAsync();
        base.OnFrameworkInitializationCompleted();
    }
}
