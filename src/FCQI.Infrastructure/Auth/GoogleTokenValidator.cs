using FCQI.Application.Auth;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace FCQI.Infrastructure.Auth;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly string? _clientId;

    public GoogleTokenValidator(IConfiguration configuration)
    {
        _clientId = configuration["Authentication:Google:ClientId"];
    }

    public async Task<GoogleUser> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_clientId))
        {
            throw new InvalidOperationException("Falta Authentication:Google:ClientId. Configúralo en User Secrets para OAuth real.");
        }

        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [_clientId]
        });

        var email = payload.Email ?? throw new InvalidOperationException("El token de Google no incluye correo.");
        if (!email.EndsWith("@uabc.edu.mx", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Solo se permiten cuentas institucionales @uabc.edu.mx.");
        }

        return new GoogleUser(email, payload.Name ?? email);
    }
}
