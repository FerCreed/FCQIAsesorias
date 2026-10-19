using System.Security.Claims;
using FCQI.Domain;

namespace FCQI.Api.Security;

/// <summary>
/// Quién hace la petición, leído del JWT y solo del JWT.
///
/// Antes los endpoints confiaban en el <c>studentId</c> que llegaba en el
/// cuerpo, así que cualquiera podía agendar o cancelar a nombre de otro. La
/// identidad ahora sale del token firmado.
/// </summary>
public class CurrentUser
{
    private readonly ClaimsPrincipal _principal;

    public CurrentUser(IHttpContextAccessor accessor)
        => _principal = accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public bool IsAuthenticated => _principal.Identity?.IsAuthenticated == true;

    public int PersonId =>
        int.TryParse(_principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("El token no trae la identidad de la persona.");

    public string Email => _principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public IReadOnlyList<string> Roles =>
        _principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

    public bool IsAdmin => _principal.IsInRole(UserRoles.Admin);
    public bool IsAdvisor => _principal.IsInRole(UserRoles.Advisor);
    public bool IsStudent => _principal.IsInRole(UserRoles.Student);
}
