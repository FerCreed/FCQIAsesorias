using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

/// <summary>Inicio de sesión institucional (BPMN 1).</summary>
public partial class LoginViewModel : PageViewModel
{
    private const string Domain = "@uabc.edu.mx";
    private readonly IFcqiApi _api;
    private readonly SessionContext _session;
    private readonly INavigator _navigator;

    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _error = string.Empty;

    public LoginViewModel(AppStatus status, IFcqiApi api, SessionContext session, INavigator navigator) : base(status)
    {
        _api = api;
        _session = session;
        _navigator = navigator;
    }

    public override string Title => "Entrar";
    public bool HasError => Error.Length > 0;

    partial void OnErrorChanged(string value) => OnPropertyChanged(nameof(HasError));

    /// <summary>Valida el correo antes de ir al servidor. Devuelve el mensaje o null si está bien.</summary>
    public static string? Validate(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email)) return $"Escribe tu correo institucional {Domain}.";
        if (!email.Trim().EndsWith(Domain, StringComparison.OrdinalIgnoreCase)) return $"Solo se aceptan cuentas {Domain}.";
        if (string.IsNullOrWhiteSpace(password)) return "Escribe tu contraseña.";
        return null;
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        Error = Validate(Email, Password) ?? string.Empty;
        if (HasError)
        {
            return;
        }

        var ok = await RunAsync(async () =>
        {
            _session.SignIn(await _api.LoginAsync(Email.Trim(), Password));
            Password = string.Empty;
        });

        if (!ok)
        {
            Error = Status.Message;
            return;
        }

        Status.Info($"Entraste como {_session.ActiveRole}.");
        await _navigator.CompleteLoginAsync();
    }
}
