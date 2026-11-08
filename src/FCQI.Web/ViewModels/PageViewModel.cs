using CommunityToolkit.Mvvm.ComponentModel;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

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

/// <summary>Navegación entre páginas. La implementa el shell.</summary>
public interface INavigator
{
    Task GoAsync(AppPage page);

    /// <summary>Lleva al acceso y, al entrar, regresa a <paramref name="returnTo"/>.</summary>
    void RequireLogin(AppPage returnTo, string message);

    /// <summary>Tras entrar: regresa a donde iba la persona, si su rol lo permite.</summary>
    Task CompleteLoginAsync();
}

/// <summary>
/// Base de todas las páginas. Patrón Método plantilla: <see cref="RunAsync"/>
/// fija el esqueleto de cualquier acción (ocupado → ejecutar → avisar o
/// mostrar el error) y cada página solo aporta el paso concreto.
/// </summary>
public abstract partial class PageViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBusy;

    protected PageViewModel(AppStatus status) => Status = status;

    protected AppStatus Status { get; }

    public abstract string Title { get; }

    /// <summary>Gancho: cada vez que la página se muestra.</summary>
    public virtual Task OnNavigatedToAsync() => Task.CompletedTask;

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
            Status.Error(ex.Message);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
