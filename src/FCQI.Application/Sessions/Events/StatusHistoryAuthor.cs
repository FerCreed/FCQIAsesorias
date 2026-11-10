using FCQI.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Events;

/// <summary>
/// Anota quién hizo cada cambio de estado en historial_estados_sesion. Las
/// filas las inserta un trigger de la base, que no conoce al usuario; sin esto
/// el reporte individual mostraba "Sistema" en todos los cambios.
/// </summary>
public class StatusHistoryAuthor : ISessionObserver
{
    private readonly IApplicationDbContext _context;

    public StatusHistoryAuthor(IApplicationDbContext context) => _context = context;

    public async Task OnChangedAsync(SessionChanged change, CancellationToken cancellationToken)
    {
        var row = await _context.SessionStatusChanges
            .Where(h => h.SessionId == change.SessionId && h.ToStatusId == change.To && h.ChangedBy == null)
            .OrderByDescending(h => h.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (row is not null)
        {
            row.ChangedBy = change.ChangedBy;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
