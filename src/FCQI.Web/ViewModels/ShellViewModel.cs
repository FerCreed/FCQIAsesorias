using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

/// <summary>Entrada del menú: a qué página lleva, con qué ícono y cuándo se muestra.</summary>
public partial class NavItem : ObservableObject
{
    public NavItem(string label, AppPage page, string icon, Func<SessionContext, bool> isVisible)
    {
        (Label, Page, IsVisibleFor) = (label, page, isVisible);
        Icon = Application.Current?.TryFindResource($"Icon.{icon}", out var geometry) == true ? geometry as Geometry : null;
    }

    public string Label { get; }
    public AppPage Page { get; }
    public Geometry? Icon { get; }
    public Func<SessionContext, bool> IsVisibleFor { get; }

    [ObservableProperty] private bool _isCurrent;
}

/// <summary>
/// Contenedor de la app (Composición): arma el menú, decide qué página se
/// muestra, protege las que exigen sesión o rol y controla el zoom. Las
/// páginas se crean con la fábrica de DI, una vez cada una.
/// </summary>
public partial class ShellViewModel : ObservableObject, INavigator
{
    public const double MinZoom = 0.8;
    public const double MaxZoom = 2.0;
    private const string ZoomKey = "fcqi.zoom";

    private readonly Func<AppPage, PageViewModel> _pages;
    private readonly IFcqiApi _api;
    private readonly IPlatformServices _platform;
    private readonly NavItem[] _menu;
    private AppPage _returnTo = AppPage.Home;
    private AppPage _current = AppPage.Home;

    [ObservableProperty] private PageViewModel? _currentPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZoomText))]
    private double _zoom = 1;

    public ShellViewModel(
        Func<AppPage, PageViewModel> pages, SessionContext session, AppStatus status, IFcqiApi api, IPlatformServices platform)
    {
        _pages = pages;
        _api = api;
        _platform = platform;
        Session = session;
        Status = status;

        _menu =
        [
            new("Inicio", AppPage.Home, "Home", _ => true),
            new("Buscar", AppPage.Catalog, "Search", _ => true),
            new("Mis asesorías", AppPage.Sessions, "Calendar", s => s.IsStudent),
            new("Solicitudes", AppPage.Sessions, "Board", s => s.IsAdvisor),
            new("Mis horarios", AppPage.Availability, "Clock", s => s.IsAdvisor),
            new("Dirección", AppPage.Admin, "People", s => s.IsAdmin),
            new("Reportes", AppPage.Reports, "DataBar", s => s.IsAdmin)
        ];

        if (double.TryParse(platform.GetSetting(ZoomKey), NumberStyles.Float, CultureInfo.InvariantCulture, out var saved))
        {
            Zoom = Math.Clamp(saved, MinZoom, MaxZoom);
        }

        Session.PropertyChanged += OnSessionChanged;
        RefreshMenu();
    }

    public SessionContext Session { get; }
    public AppStatus Status { get; }
    public ObservableCollection<NavItem> Menu { get; } = [];
    public string ZoomText => $"{Math.Round(Zoom * 100)} %";

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

    /// <summary>BPMN 17: borra el token y vacía listas y selecciones para que nadie vea datos ajenos.</summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        Session.SignOut();
        foreach (var page in Enum.GetValues<AppPage>().Select(_pages).OfType<IResettable>())
        {
            page.Reset();
        }

        Status.Info("Sesión cerrada. El catálogo sigue disponible.");
        await GoAsync(AppPage.Home);
    }

    [RelayCommand] private void ZoomIn() => SetZoom(Zoom + 0.1);
    [RelayCommand] private void ZoomOut() => SetZoom(Zoom - 0.1);
    [RelayCommand] private void ZoomReset() => SetZoom(1);

    private void SetZoom(double value)
    {
        Zoom = Math.Round(Math.Clamp(value, MinZoom, MaxZoom), 1);
        _platform.SetSetting(ZoomKey, Zoom.ToString(CultureInfo.InvariantCulture));
        Status.Info($"Tamaño de la interfaz: {ZoomText}.");
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
        if (e.PropertyName is not nameof(SessionContext.ActiveRole))
        {
            return;
        }

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
