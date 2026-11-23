using FCQI.Application.Common;
using FCQI.Application.Auth;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace FCQI.Infrastructure.Auth;

// Adaptador de IGoogleTokenValidator sobre la librería oficial
// Google.Apis.Auth, que verifica firma y vigencia contra las claves públicas
// de Google sin implementar criptografía propia.
public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly string? _clientId;

    // Lee el Client ID de OAuth; puede faltar en entornos sin Google configurado.
    public GoogleTokenValidator(IConfiguration configuration)
    {
        _clientId = configuration["Authentication:Google:ClientId"];
    }

    // Valida el token y devuelve el correo y el nombre. Restringe a cuentas
    // @uabc.edu.mx porque el programa es exclusivo de la universidad.
    public async Task<GoogleUser> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_clientId))
        {
            throw new BusinessRuleException("Falta Authentication:Google:ClientId. Configúralo en User Secrets para OAuth real.");
        }

        // Fijar la audiencia impide aceptar tokens emitidos para otra aplicación.
        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [_clientId]
        });

        var email = payload.Email ?? throw new BusinessRuleException("El token de Google no incluye correo.");
        if (!email.EndsWith("@uabc.edu.mx", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("Solo se permiten cuentas institucionales @uabc.edu.mx.");
        }

        return new GoogleUser(email, payload.Name ?? email);
    }
}
