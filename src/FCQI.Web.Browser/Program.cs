using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using FCQI.Web;

[assembly: SupportedOSPlatform("browser")]

internal sealed partial class Program
{
    /// <summary>
    /// main.js pasa la URL de la página como primer argumento. La API vive en
    /// el mismo origen (nginx sirve /api), salvo que se indique otra con
    /// <c>?api=http://localhost:5016</c>, útil al depurar la API fuera de Docker.
    /// </summary>
    private static Task Main(string[] args)
    {
        App.ApiBaseAddress = ApiAddress(args.FirstOrDefault());
        return BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

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

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
