using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

/// <summary>Portada: saluda y ofrece los accesos del rol activo.</summary>
public partial class HomeViewModel : PageViewModel
{
    private readonly INavigator _navigator;

    public HomeViewModel(AppStatus status, SessionContext session, INavigator navigator) : base(status)
    {
        Session = session;
        _navigator = navigator;
        Session.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Greeting));
    }

    public override string Title => "Inicio";
    public SessionContext Session { get; }

    public string Greeting => Session.IsLoggedIn ? $"Hola, {Session.DisplayName}" : "Asesorías académicas FCQI";

    [RelayCommand]
    private Task GoAsync(AppPage page) => _navigator.GoAsync(page);

    [RelayCommand]
    private void Login() => _navigator.RequireLogin(AppPage.Home, "Entra con tu correo institucional @uabc.edu.mx.");
}
