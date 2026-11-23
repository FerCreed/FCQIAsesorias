using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FCQI.Web.Models;

namespace FCQI.Web.Services;

// Quién usa la app y con qué rol. Única instancia (singleton en DI) y
// observable: el shell y las páginas reaccionan a sus cambios en lugar de
// recibir avisos a mano.
public partial class SessionContext : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoggedIn), nameof(Token), nameof(PersonId), nameof(DisplayName))]
    private AuthResultItem? _auth;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStudent), nameof(IsAdvisor), nameof(IsAdmin))]
    private string? _activeRole;

    // Roles de la persona; observable para que el selector de rol se actualice solo.
    public ObservableCollection<string> Roles { get; } = [];

    // Propiedades derivadas: se recalculan al cambiar Auth o ActiveRole (ver NotifyPropertyChangedFor).
    public bool IsLoggedIn => Auth is not null;
    public string? Token => Auth?.Token;
    public int PersonId => Auth?.ProfileId ?? 0;
    public string DisplayName => Auth?.FullName ?? string.Empty;
    public bool IsStudent => ActiveRole == UserRoles.Student;
    public bool IsAdvisor => ActiveRole == UserRoles.Advisor;
    public bool IsAdmin => ActiveRole == UserRoles.Admin;
    public bool HasSeveralRoles => Roles.Count > 1;

    // Inicia la sesión con la respuesta de la API y activa el rol
    // predeterminado. Tolera respuestas sin lista de roles usando el rol único.
    public void SignIn(AuthResultItem auth)
    {
        var roles = auth.Roles.Count > 0 ? auth.Roles : [auth.Role];
        Roles.Clear();
        foreach (var role in roles)
        {
            Roles.Add(role);
        }

        Auth = auth;
        ActiveRole = roles.Contains(auth.Role) ? auth.Role : roles[0];
        OnPropertyChanged(nameof(HasSeveralRoles));
    }

    // Cierra la sesión y descarta el token de memoria.
    public void SignOut()
    {
        Auth = null;
        ActiveRole = null;
        Roles.Clear();
        OnPropertyChanged(nameof(HasSeveralRoles));
    }
}
