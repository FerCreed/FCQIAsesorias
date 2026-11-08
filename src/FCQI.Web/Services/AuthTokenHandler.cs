using System.Net.Http.Headers;

namespace FCQI.Web.Services;

/// <summary>
/// Proxy de las peticiones HTTP: añade el token de la sesión activa. Así ningún
/// ViewModel ni la fachada tocan cabeceras de autenticación.
/// </summary>
public class AuthTokenHandler : DelegatingHandler
{
    private readonly SessionContext _session;

    public AuthTokenHandler(SessionContext session) => _session = session;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_session.Token is { Length: > 0 } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
