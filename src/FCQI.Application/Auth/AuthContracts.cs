namespace FCQI.Application.Auth;

public record GoogleUser(string Email, string Name);

public interface IGoogleTokenValidator
{
    Task<GoogleUser> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Role es el rol predeterminado (el de mayor alcance). Roles trae todos los
/// que tiene la persona: un asesor par aparece como Asesor y como Alumno.
/// </summary>
public record AuthResult(
    string Token,
    string Role,
    int ProfileId,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles);

public record DemoProfileDto(string Role, int ProfileId, string FullName, string Email);
