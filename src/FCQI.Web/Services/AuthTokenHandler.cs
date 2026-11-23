using System.Net.Http.Headers;

namespace FCQI.Web.Services;

// Proxy de las peticiones HTTP: añade el token de la sesión activa. Así ningún
// ViewModel ni la fachada tocan cabeceras de autenticación.
public class AuthTokenHandler : DelegatingHandler
{
    private readonly SessionContext _session;

    // Recibe la sesión de la que lee el token en cada petición.
    public AuthTokenHandler(SessionContext session) => _session = session;

    // Añade la cabecera Bearer solo si hay token; las peticiones públicas
    // (catálogo, login) salen sin ella.
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_session.Token is { Length: > 0 } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
