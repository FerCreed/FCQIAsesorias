namespace FCQI.Application.Auth;

public record GoogleUser(string Email, string Name);

public interface IGoogleTokenValidator
{
    Task<GoogleUser> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}

public record AuthResult(string Token, string Role, int ProfileId, string FullName, string Email);

public record DemoProfileDto(string Role, int ProfileId, string FullName, string Email);
