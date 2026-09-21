using FCQI.Application.Interfaces;
using FCQI.Application.Sessions.Dtos;
using FCQI.Domain;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Commands;

public class UpdateSessionStatusCommandHandler
{
    private readonly IApplicationDbContext _context;

    public UpdateSessionStatusCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task HandleAsync(int sessionId, string status, CancellationToken cancellationToken = default)
    {
        var allowed = new[] { SessionStatuses.Pending, SessionStatuses.Confirmed, SessionStatuses.Cancelled, SessionStatuses.Rejected };
        if (!allowed.Contains(status))
        {
            throw new InvalidOperationException("Estado no válido.");
        }

        var session = await _context.AdvisorySessions.SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
                      ?? throw new InvalidOperationException("La solicitud no existe.");
        session.Status = status;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
