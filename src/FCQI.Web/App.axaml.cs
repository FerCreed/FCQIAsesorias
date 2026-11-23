using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FCQI.Web.ViewModels;
using FCQI.Web.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FCQI.Web;

// Aplicación Avalonia compartida por navegador y escritorio. Construye el
// contenedor de servicios y monta el shell según el tipo de ciclo de vida.
public partial class App : Application
{
    // Dirección de la API. La fija cada anfitrión (navegador o escritorio) antes de arrancar.
    public static Uri ApiBaseAddress { get; set; } = new("http://localhost:8080");

    // Carga los estilos y recursos declarados en App.axaml.
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    // Crea la vista raíz y sus servicios. La vista se pasa como función a los
    // servicios porque el TopLevel (para diálogos) solo existe tras montarla.
    public override void OnFrameworkInitializationCompleted()
    {
        var view = new ShellView();
        var services = AppServices.Build(ApiBaseAddress, () => TopLevel.GetTopLevel(view));
        var shell = services.GetRequiredService<ShellViewModel>();
        view.DataContext = shell;

        // Escritorio usa ventana; el navegador, una sola vista sin ventana.
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

        // Arranque asíncrono sin esperar: la interfaz se muestra mientras se cargan los datos iniciales.
        _ = shell.StartAsync();
        base.OnFrameworkInitializationCompleted();
    }
}
