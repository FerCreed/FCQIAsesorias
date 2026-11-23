using Avalonia.Controls;
using FCQI.Web.Services;
using FCQI.Web.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FCQI.Web;

// Raíz de composición del cliente. Todo es singleton: hay una sola persona
// usando la app, una sesión y una instancia de cada página (que así conserva
// su estado al navegar).
public static class AppServices
{
    // Registra servicios, fachada de la API y páginas. Es el único lugar que
    // conoce las clases concretas; el resto depende de interfaces.
    public static ServiceProvider Build(Uri apiBaseAddress, Func<TopLevel?> topLevel)
    {
        var services = new ServiceCollection();

        services.AddSingleton<SessionContext>();
        services.AddSingleton<AppStatus>();
        // Un solo HttpClient con el proxy de autenticación delante; 20 s de espera
        // para que una red lenta muestre error en vez de quedarse colgada.
        services.AddSingleton(sp => new HttpClient(new AuthTokenHandler(sp.GetRequiredService<SessionContext>())
        {
            InnerHandler = new HttpClientHandler()
        })
        {
            BaseAddress = apiBaseAddress,
            Timeout = TimeSpan.FromSeconds(20)
        });
        // Una sola instancia del cliente detrás de dos interfaces; el catálogo pasa por el decorador de caché.
        services.AddSingleton<FcqiApiClient>();
        services.AddSingleton<IFcqiApi>(sp => sp.GetRequiredService<FcqiApiClient>());
        services.AddSingleton<ICatalogApi>(sp => new CachedCatalogApi(sp.GetRequiredService<FcqiApiClient>()));
        services.AddSingleton<IPlatformServices>(new AvaloniaPlatformServices(topLevel));

        // El shell es también el navegador entre páginas.
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<INavigator>(sp => sp.GetRequiredService<ShellViewModel>());
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<CatalogViewModel>();
        services.AddSingleton<SessionsViewModel>();
        services.AddSingleton<AvailabilityViewModel>();
        services.AddSingleton<AdminViewModel>();
        services.AddSingleton<ReportsViewModel>();

        // Fábrica de páginas: el shell pide por AppPage y no conoce las clases concretas.
        services.AddSingleton<Func<AppPage, PageViewModel>>(sp => page => page switch
        {
            AppPage.Home => sp.GetRequiredService<HomeViewModel>(),
            AppPage.Login => sp.GetRequiredService<LoginViewModel>(),
            AppPage.Catalog => sp.GetRequiredService<CatalogViewModel>(),
            AppPage.Sessions => sp.GetRequiredService<SessionsViewModel>(),
            AppPage.Availability => sp.GetRequiredService<AvailabilityViewModel>(),
            AppPage.Admin => sp.GetRequiredService<AdminViewModel>(),
            AppPage.Reports => sp.GetRequiredService<ReportsViewModel>(),
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        });

        return services.BuildServiceProvider();
    }
}
