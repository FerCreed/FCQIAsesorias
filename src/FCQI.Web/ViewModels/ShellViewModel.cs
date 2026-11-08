using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

/// <summary>Entrada del menú: a qué página lleva y cuándo se muestra.</summary>
public partial class NavItem : ObservableObject
{
    public NavItem(string label, AppPage page, Func<SessionContext, bool> isVisible)
        => (Label, Page, IsVisibleFor) = (label, page, isVisible);

    public string Label { get; }
    public AppPage Page { get; }
    public Func<SessionContext, bool> IsVisibleFor { get; }

    [ObservableProperty] private bool _isCurrent;
}

/// <summary>
/// Contenedor de la app (Composición): arma el menú, decide qué página se
/// muestra y protege las que exigen sesión o rol. Las páginas se crean con la
/// fábrica de DI, una sola vez cada una, y conservan su estado entre visitas.
/// </summary>
public partial class ShellViewModel : ObservableObject, INavigator
{
    private readonly Func<AppPage, PageViewModel> _pages;
    private readonly IFcqiApi _api;
    private readonly NavItem[] _menu;
    private AppPage _returnTo = AppPage.Home;
    private AppPage _current = AppPage.Home;

    [ObservableProperty] private PageViewModel? _currentPage;

    public ShellViewModel(Func<AppPage, PageViewModel> pages, SessionContext session, AppStatus status, IFcqiApi api)
    {
        _pages = pages;
        _api = api;
        Session = session;
        Status = status;

        _menu =
        [
            new("Inicio", AppPage.Home, _ => true),
            new("Catálogo", AppPage.Catalog, _ => true),
            new("Mis asesorías", AppPage.Sessions, s => s.IsStudent),
            new("Solicitudes", AppPage.Sessions, s => s.IsAdvisor),
            new("Mis horarios", AppPage.Availability, s => s.IsAdvisor),
            new("Dirección", AppPage.Admin, s => s.IsAdmin),
            new("Reportes", AppPage.Reports, s => s.IsAdmin)
        ];

        Session.PropertyChanged += OnSessionChanged;
        RefreshMenu();
    }

    public SessionContext Session { get; }
    public AppStatus Status { get; }
    public ObservableCollection<NavItem> Menu { get; } = [];

    public async Task StartAsync()
    {
        await GoAsync(AppPage.Home);
        try
        {
            await _api.GetAuthConfigAsync();
            Status.Info("Consulta el catálogo sin cuenta. Para pedir una cita, entra con tu correo @uabc.edu.mx.");
        }
        catch (Exception ex)
        {
            Status.Error($"No se pudo conectar con el servidor ({ex.Message}). Revisa que la API esté en línea.");
        }
    }

    [RelayCommand]
    public async Task GoAsync(AppPage page)
    {
        var denied = Guard(page);
        if (denied is not null)
        {
            if (!Session.IsLoggedIn)
            {
                RequireLogin(page, denied);
            }
            else
            {
                Status.Error(denied);
            }

            return;
        }

        _current = page;
        CurrentPage = _pages(page);
        foreach (var item in Menu)
        {
            item.IsCurrent = item.Page == page;
        }

        await CurrentPage.OnNavigatedToAsync();
    }

    public void RequireLogin(AppPage returnTo, string message)
    {
        _returnTo = returnTo;
        Status.Info(message);
        _ = GoAsync(AppPage.Login);
    }

    public Task CompleteLoginAsync()
    {
        var target = Guard(_returnTo) is null ? _returnTo : AppPage.Home;
        _returnTo = AppPage.Home;
        return GoAsync(target);
    }

    [RelayCommand]
    private void OpenLogin() => RequireLogin(_current == AppPage.Login ? AppPage.Home : _current, "Entra con tu correo institucional @uabc.edu.mx.");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        Session.SignOut();
        Status.Info("Sesión cerrada. El catálogo sigue disponible.");
        await GoAsync(AppPage.Home);
    }

    /// <summary>Por qué no se puede entrar a una página, o null si se puede.</summary>
    private string? Guard(AppPage page) => page switch
    {
        AppPage.Sessions when !Session.IsLoggedIn => "Entra para ver tus asesorías.",
        AppPage.Availability when !Session.IsAdvisor => Session.IsLoggedIn ? "Esta sección es para asesores." : "Entra con tu cuenta de asesor.",
        AppPage.Admin or AppPage.Reports when !Session.IsAdmin => Session.IsLoggedIn ? "Esta sección es para dirección del programa." : "Entra con una cuenta de dirección.",
        _ => null
    };

    private void OnSessionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SessionContext.ActiveRole))
        {
            RefreshMenu();
            // Al cambiar de rol, la página actual puede dejar de estar permitida.
            if (Guard(_current) is not null)
            {
                _ = GoAsync(AppPage.Home);
            }
            else
            {
                _ = CurrentPage?.OnNavigatedToAsync();
            }
        }
    }

    private void RefreshMenu()
    {
        Menu.Clear();
        foreach (var item in _menu.Where(i => i.IsVisibleFor(Session)))
        {
            item.IsCurrent = item.Page == _current;
            Menu.Add(item);
        }
    }
}
