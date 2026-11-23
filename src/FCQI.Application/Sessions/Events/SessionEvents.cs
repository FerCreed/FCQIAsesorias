using Microsoft.Extensions.Logging;

namespace FCQI.Application.Sessions.Events;

// Una asesoría cambió de estado. From es null cuando se acaba de crear.
public record SessionChanged(long SessionId, byte? From, byte To, int ChangedBy);

// Patrón Observador: reacciona a los cambios de estado de una asesoría.
public interface ISessionObserver
{
    // Se invoca después de guardar el cambio, una vez por observador registrado.
    Task OnChangedAsync(SessionChanged change, CancellationToken cancellationToken);
}

// Avisa a todos los observadores registrados. El cambio ya se guardó cuando
// esto corre, así que el fallo de un observador (p. ej. el correo) se registra
// y no tumba la petición: la asesoría sí cambió.
public class SessionEventPublisher
{
    private readonly IEnumerable<ISessionObserver> _observers;
    private readonly ILogger<SessionEventPublisher> _logger;

    // Recibe todos los observadores registrados en el contenedor, en el orden
    // de registro, y un logger para dejar rastro de los fallos.
    public SessionEventPublisher(IEnumerable<ISessionObserver> observers, ILogger<SessionEventPublisher> logger)
    {
        _observers = observers;
        _logger = logger;
    }

    // Notifica en secuencia, no en paralelo, porque los observadores comparten
    // el DbContext de la petición y el orden importa (el enlace antes del correo).
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
