using CommunityToolkit.Mvvm.ComponentModel;

namespace FCQI.Web.Services;

/// <summary>Mensaje de estado global que muestra el shell (éxito o error de la última acción).</summary>
public partial class AppStatus : ObservableObject
{
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private bool _isError;

    public void Info(string message) => (Message, IsError) = (message, false);
    public void Error(string message) => (Message, IsError) = (message, true);
    public void Clear() => Message = string.Empty;
}
