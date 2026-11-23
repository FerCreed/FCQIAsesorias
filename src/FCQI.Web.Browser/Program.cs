using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using FCQI.Web;
using FCQI.Web.Browser;
using FCQI.Web.Services;

[assembly: SupportedOSPlatform("browser")]

// Arranque del cliente en WebAssembly. Conecta las funciones de JavaScript
// con HostHooks y monta la app en el elemento #out.
internal sealed partial class Program
{
    // main.js pasa la URL de la página como primer argumento. La API vive en
    // el mismo origen (nginx sirve /api), salvo que se indique otra con
    // ?api=http://localhost:5016, útil al depurar la API fuera de Docker.
    private static Task Main(string[] args)
    {
        App.ApiBaseAddress = ApiAddress(args.FirstOrDefault());
        HostHooks.Download = (name, mime, bytes) => BrowserInterop.Download(name, mime, Convert.ToBase64String(bytes));
        HostHooks.GetSetting = BrowserInterop.GetSetting;
        HostHooks.SetSetting = BrowserInterop.SetSetting;
        return BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    // Deduce la URL de la API a partir de la página: el parámetro api
    // si es válido; si no, el mismo origen. Sin URL de página usa el puerto
    // del contenedor local.
    private static Uri ApiAddress(string? pageUrl)
    {
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var page))
        {
            return new Uri("http://localhost:8080");
        }

        var api = System.Web.HttpUtility.ParseQueryString(page.Query)["api"];
        return Uri.TryCreate(api, UriKind.Absolute, out var custom)
            ? custom
            : new Uri(page.GetLeftPart(UriPartial.Authority));
    }

    // Configuración mínima de Avalonia para el navegador; pública porque la usan las herramientas de diseño.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
