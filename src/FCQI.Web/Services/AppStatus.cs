using CommunityToolkit.Mvvm.ComponentModel;

namespace FCQI.Web.Services;

// Mensaje de estado global que muestra el shell (éxito o error de la última acción).
public partial class AppStatus : ObservableObject
{
    // ObservableProperty genera las propiedades Message e IsError con notificación de cambio.
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private bool _isError;

    // Asignación por tupla: mensaje y tipo cambian juntos, sin un estado intermedio inconsistente.
    public void Info(string message) => (Message, IsError) = (message, false);
    public void Error(string message) => (Message, IsError) = (message, true);
    public void Clear() => Message = string.Empty;
}
