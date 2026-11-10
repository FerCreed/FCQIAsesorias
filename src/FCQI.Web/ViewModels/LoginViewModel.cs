using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FCQI.Web.Models;
using FCQI.Web.Services;

namespace FCQI.Web.ViewModels;

/// <summary>Inicio de sesión institucional (BPMN 1).</summary>
public partial class LoginViewModel : PageViewModel, IResettable
{
    private const string Domain = "@uabc.edu.mx";
    private readonly IFcqiApi _api;
    private readonly SessionContext _session;
    private readonly INavigator _navigator;

    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private DemoProfileItem? _selectedProfile;
    [ObservableProperty] private string _error = string.Empty;

    public LoginViewModel(AppStatus status, IFcqiApi api, SessionContext session, INavigator navigator) : base(status)
    {
        _api = api;
        _session = session;
        _navigator = navigator;
    }

    public override string Title => "Entrar";
    public bool HasError => Error.Length > 0;

    /// <summary>Perfiles para entrar eligiendo de una lista: en el celular evita escribir el correo.</summary>
    public ObservableCollection<DemoProfileItem> Profiles { get; } = [];
    public bool HasProfiles => Profiles.Count > 0;

    partial void OnErrorChanged(string value) => OnPropertyChanged(nameof(HasError));

    partial void OnSelectedProfileChanged(DemoProfileItem? value)
    {
        if (value is not null)
        {
            Email = value.Email;
            Error = string.Empty;
        }
    }

    public override async Task OnNavigatedToAsync()
    {
        if (Profiles.Count > 0)
        {
            return;
        }

        try
        {
            // Las cuentas *.prueba@ (una por rol) primero: entre cientos de perfiles se perderían.
            var profiles = await _api.GetDemoProfilesAsync();
            foreach (var profile in profiles.OrderBy(p => !p.Email.Contains(".prueba@", StringComparison.OrdinalIgnoreCase)))
            {
                Profiles.Add(profile);
            }
        }
        catch (Exception)
        {
            // Servidor sin acceso por correo (solo Google): la lista no aplica y el formulario lo explica al entrar.
        }

        OnPropertyChanged(nameof(HasProfiles));
    }

    /// <summary>BPMN 17: al salir no queda a la vista el correo de quien entró.</summary>
    public void Reset()
    {
        SelectedProfile = null;
        Email = string.Empty;
        Error = string.Empty;
    }

    /// <summary>Valida el correo antes de ir al servidor. Devuelve el mensaje o null si está bien.</summary>
    public static string? Validate(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return $"Escribe tu correo institucional {Domain}.";
        if (!email.Trim().EndsWith(Domain, StringComparison.OrdinalIgnoreCase)) return $"Solo se aceptan cuentas {Domain}.";
        return null;
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        Error = Validate(Email) ?? string.Empty;
        if (HasError)
        {
            return;
        }

        var ok = await RunAsync(async () =>
        {
            _session.SignIn(await _api.LoginAsync(Email.Trim()));
            // Si eligió "Alumno" de un asesor par, entra con ese rol y no con el predeterminado.
            var chosen = SelectedProfile;
            if (chosen is not null && string.Equals(chosen.Email, Email.Trim(), StringComparison.OrdinalIgnoreCase)
                && _session.Roles.Contains(chosen.Role))
            {
                _session.ActiveRole = chosen.Role;
            }
        });

        if (!ok)
        {
            Error = Status.Message;
            return;
        }

        Status.Clear(); // quién está conectado se ve en el menú ☰, no en la franja de avisos
        await _navigator.CompleteLoginAsync();
    }
}
