using FCQI.Application.Interfaces;
using FCQI.Domain;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Events;

// BPMN 10: al confirmar una cita virtual se genera su enlace de reunión y se
// guarda en la cita. Las presenciales no llevan enlace.
public class MeetingLinkObserver : ISessionObserver
{
    private readonly IApplicationDbContext _context;
    private readonly IMeetingLinkProvider _provider;

    // Recibe el contexto de datos y el proveedor de enlaces por inyección.
    public MeetingLinkObserver(IApplicationDbContext context, IMeetingLinkProvider provider)
    {
        _context = context;
        _provider = provider;
    }

    // Genera y guarda el enlace solo al confirmar una cita virtual que aún no lo tiene.
    public async Task OnChangedAsync(SessionChanged change, CancellationToken cancellationToken)
    {
        // Ignora cualquier otro cambio de estado.
        if (change.To != SessionStatusIds.Confirmed)
        {
            return;
        }

        var session = await _context.AdvisorySessions
            .Include(s => s.Availability).ThenInclude(a => a.Location).ThenInclude(l => l.Modality)
            .SingleAsync(s => s.Id == change.SessionId, cancellationToken);

        // No reemplaza un enlace existente: el alumno pudo haberlo recibido ya por correo.
        var location = session.Availability.Location;
        if (location.Modality.Name != Modalities.Virtual || session.MeetingLink is not null)
        {
            return;
        }

        session.MeetingLink = await _provider.CreateLinkAsync(session, location, cancellationToken);
        if (session.MeetingLink is not null)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
