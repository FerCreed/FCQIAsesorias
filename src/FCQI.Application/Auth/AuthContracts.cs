namespace FCQI.Application.Auth;

// Datos mínimos que se extraen de un token de Google ya validado.
public record GoogleUser(string Email, string Name);

// Puerto para validar el ID token de Google. Vive en aplicación y se implementa
// en infraestructura, de modo que el caso de uso no depende de la librería de Google.
public interface IGoogleTokenValidator
{
    // Valida firma, audiencia y vigencia del token; lanza si no es válido.
    Task<GoogleUser> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}

// Role es el rol predeterminado (el de mayor alcance). Roles trae todos los
// que tiene la persona: un asesor par aparece como Asesor y como Alumno.
public record AuthResult(
    string Token,
    string Role,
    int ProfileId,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles);

// Perfil seleccionable en el login de demostración: una fila por persona y rol.
public record DemoProfileDto(string Role, int ProfileId, string FullName, string Email);
