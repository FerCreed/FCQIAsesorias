using CommunityToolkit.Mvvm.ComponentModel;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

// Páginas de la aplicación; el shell las resuelve con la fábrica registrada en AppServices.
public enum AppPage
{
    Home,
    Login,
    Catalog,
    Sessions,
    Availability,
    Admin,
    Reports
}

// Navegación entre páginas. La implementa el shell.
public interface INavigator
{
    // Muestra la página indicada, respetando los permisos del rol activo.
    Task GoAsync(AppPage page);

    // Lleva al acceso y, al entrar, regresa a returnTo.
    void RequireLogin(AppPage returnTo, string message);

    // Tras entrar: regresa a donde iba la persona, si su rol lo permite.
    Task CompleteLoginAsync();
}

// Base de todas las páginas. Patrón Método plantilla: RunAsync
// fija el esqueleto de cualquier acción (ocupado → ejecutar → avisar o
// mostrar el error) y cada página solo aporta el paso concreto.
public abstract partial class PageViewModel : ObservableObject
{
    // Genera IsBusy: la vista lo usa para mostrar el indicador y deshabilitar botones.
    [ObservableProperty] private bool _isBusy;

    // Recibe la franja de estado global compartida por todas las páginas.
    protected PageViewModel(AppStatus status) => Status = status;

    // Franja de avisos donde se reportan éxitos y errores.
    protected AppStatus Status { get; }

    // Título que muestra el shell para la página.
    public abstract string Title { get; }

    // Gancho: cada vez que la página se muestra.
    public virtual Task OnNavigatedToAsync() => Task.CompletedTask;

    // Ejecuta una acción de la página con el esqueleto común. Devuelve false si
    // falló o si ya había otra en curso; la segunda pulsación se ignora para
    // no duplicar peticiones (doble clic en "Solicitar").
    protected async Task<bool> RunAsync(Func<Task> action, string? success = null)
    {
        if (IsBusy)
        {
            return false;
        }

        IsBusy = true;
        try
        {
            await action();
            if (success is not null)
            {
                Status.Info(success);
            }

            return true;
        }
        catch (Exception ex)
        {
            // Los mensajes vienen ya redactados para el usuario por FcqiApiClient.
            Status.Error(ex.Message);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
