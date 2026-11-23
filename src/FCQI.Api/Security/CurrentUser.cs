using System.Security.Claims;
using FCQI.Application.Common;
using FCQI.Domain;

namespace FCQI.Api.Security;

// Quién hace la petición, leído del JWT y solo del JWT.
//
// Antes los endpoints confiaban en el studentId que llegaba en el
// cuerpo, así que cualquiera podía agendar o cancelar a nombre de otro. La
// identidad ahora sale del token firmado.
public class CurrentUser
{
    private readonly ClaimsPrincipal _principal;

    // Toma el usuario del contexto HTTP; fuera de una petición usa un
    // principal vacío en vez de null para no propagar referencias nulas.
    public CurrentUser(IHttpContextAccessor accessor)
        => _principal = accessor.HttpContext?.User ?? new ClaimsPrincipal();

    // Si la petición trae un token válido.
    public bool IsAuthenticated => _principal.Identity?.IsAuthenticated == true;

    // Identificador de la persona según el token. Lanza si falta, porque
    // seguir con un id por omisión haría actuar a la petición como otra persona.
    public int PersonId =>
        int.TryParse(_principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("El token no trae la identidad de la persona.");

    // Correo institucional del token; cadena vacía si no viene.
    public string Email => _principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    // Todos los roles del token (un asesor par trae Asesor y Alumno).
    public IReadOnlyList<string> Roles =>
        _principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

    // Atajos por rol, evaluados contra los claims firmados del token.
    public bool IsAdmin => _principal.IsInRole(UserRoles.Admin);
    public bool IsAdvisor => _principal.IsInRole(UserRoles.Advisor);
    public bool IsStudent => _principal.IsInRole(UserRoles.Student);

    // La identidad en el formato que entienden los casos de uso.
    public Caller ToCaller() => new(PersonId, IsAdmin, IsAdvisor, IsStudent);
}
