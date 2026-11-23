using Avalonia;
using FCQI.Web;

namespace FCQI.Desktop;

// Arranque del cliente de escritorio. Reutiliza la misma aplicación Avalonia
// que la versión web (FCQI.Web); aquí solo se elige la URL de la API y el
// ciclo de vida de escritorio.
internal static class Program
{
    // La API se toma de FCQI_API_URL; por omisión, el contenedor de
    // producción publicado en http://localhost:8080.
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
