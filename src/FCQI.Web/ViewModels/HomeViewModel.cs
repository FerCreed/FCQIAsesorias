using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

// Portada: saluda y ofrece los accesos del rol activo.
public partial class HomeViewModel : PageViewModel
{
    private readonly INavigator _navigator;

    // Recibe la sesión y el navegador por inyección. Se suscribe a la sesión
    // (Observador) para refrescar el saludo al entrar o salir.
    public HomeViewModel(AppStatus status, SessionContext session, INavigator navigator) : base(status)
    {
        Session = session;
        _navigator = navigator;
        Session.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Greeting));
    }

    // Título de la página en la barra del shell.
    public override string Title => "Inicio";

    // Sesión expuesta a la vista para mostrar los accesos según el rol.
    public SessionContext Session { get; }

    // Saludo personalizado con sesión; nombre del programa sin ella.
    public string Greeting => Session.IsLoggedIn ? $"Hola, {Session.DisplayName}" : "Asesorías académicas FCQI";

    // Comando de los accesos directos: navega a la página indicada.
    [RelayCommand]
    private Task GoAsync(AppPage page) => _navigator.GoAsync(page);

    // Lleva al login y regresa a la portada al entrar.
    [RelayCommand]
    private void Login() => _navigator.RequireLogin(AppPage.Home, "Entra con tu correo institucional @uabc.edu.mx.");
}
