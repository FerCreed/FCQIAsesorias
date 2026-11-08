using Microsoft.Extensions.Logging;

namespace FCQI.Application.Sessions.Events;

/// <summary>Una asesoría cambió de estado. <c>From</c> es null cuando se acaba de crear.</summary>
public record SessionChanged(long SessionId, byte? From, byte To, int ChangedBy);

/// <summary>Patrón Observador: reacciona a los cambios de estado de una asesoría.</summary>
public interface ISessionObserver
{
    Task OnChangedAsync(SessionChanged change, CancellationToken cancellationToken);
}

/// <summary>
/// Avisa a todos los observadores registrados. El cambio ya se guardó cuando
/// esto corre, así que el fallo de un observador (p. ej. el correo) se registra
/// y no tumba la petición: la asesoría sí cambió.
/// </summary>
public class SessionEventPublisher
{
    private readonly IEnumerable<ISessionObserver> _observers;
    private readonly ILogger<SessionEventPublisher> _logger;

    public SessionEventPublisher(IEnumerable<ISessionObserver> observers, ILogger<SessionEventPublisher> logger)
    {
        _observers = observers;
        _logger = logger;
    }

    public async Task PublishAsync(SessionChanged change, CancellationToken cancellationToken = default)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnChangedAsync(change, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Observer} falló con la asesoría {SessionId}", observer.GetType().Name, change.SessionId);
            }
        }
    }
}
