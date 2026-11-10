using FCQI.Application.Interfaces;
using FCQI.Domain;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Events;

/// <summary>
/// BPMN 10: al confirmar una cita virtual se genera su enlace de reunión y se
/// guarda en la cita. Las presenciales no llevan enlace.
/// </summary>
public class MeetingLinkObserver : ISessionObserver
{
    private readonly IApplicationDbContext _context;
    private readonly IMeetingLinkProvider _provider;

    public MeetingLinkObserver(IApplicationDbContext context, IMeetingLinkProvider provider)
    {
        _context = context;
        _provider = provider;
    }

    public async Task OnChangedAsync(SessionChanged change, CancellationToken cancellationToken)
    {
        if (change.To != SessionStatusIds.Confirmed)
        {
            return;
        }

        var session = await _context.AdvisorySessions
            .Include(s => s.Availability).ThenInclude(a => a.Location).ThenInclude(l => l.Modality)
            .SingleAsync(s => s.Id == change.SessionId, cancellationToken);

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
