using Avalonia;
using FCQI.Web;

namespace FCQI.Desktop;

internal static class Program
{
    /// <summary>
    /// La API se toma de FCQI_API_URL; por omisión, el contenedor de
    /// producción publicado en http://localhost:8080.
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        App.ApiBaseAddress = new Uri(Environment.GetEnvironmentVariable("FCQI_API_URL") ?? "http://localhost:8080");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Lo usa también el previsualizador de XAML del IDE.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
