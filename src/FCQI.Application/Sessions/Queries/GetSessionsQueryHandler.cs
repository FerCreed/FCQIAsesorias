using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Application.Sessions.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Queries;

public class GetSessionsQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IClock _clock;

    public GetSessionsQueryHandler(IApplicationDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    /// <param name="scope">
    /// <c>upcoming</c>: citas por venir que siguen vivas (panel "Mis asesorías", BPMN 19).
    /// <c>history</c>: las pasadas o cerradas (BPMN 14). Cualquier otro valor: todas.
    /// </param>
    public async Task<IReadOnlyList<SessionDto>> HandleAsync(
        int? studentId, int? advisorId, string? status, Caller caller,
        string? scope = null, CancellationToken cancellationToken = default)
    {
        // Dirección ve todo. Los demás solo lo suyo: si no piden un filtro, se
        // les impone; si piden el de otra persona, se rechaza en vez de
        // devolver una lista vacía, que ocultaría el motivo.
        if (!caller.IsAdmin)
        {
            if (caller.IsAdvisor && advisorId is null && studentId is null)
            {
                advisorId = caller.PersonId;
            }
            else if (caller.IsStudent && studentId is null && advisorId is null)
            {
                studentId = caller.PersonId;
            }

            if ((studentId is not null && studentId != caller.PersonId) ||
                (advisorId is not null && advisorId != caller.PersonId))
            {
                throw new ForbiddenOperationException("Solo puedes consultar tus propias asesorías.");
            }
        }

        var query = _context.AdvisorySessions.AsNoTracking();

        if (studentId is not null)
        {
            query = query.Where(s => s.StudentId == studentId);
        }

        if (advisorId is not null)
        {
            query = query.Where(s => s.AdvisorId == advisorId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status.Name == status);
        }

        var now = _clock.UtcNow;
        query = scope switch
        {
            "upcoming" => query.Where(s => s.ScheduledAt >= now && s.Status.IsActive)
                               .OrderBy(s => s.ScheduledAt),
            "history" => query.Where(s => s.ScheduledAt < now || !s.Status.IsActive)
                              .OrderByDescending(s => s.ScheduledAt),
            _ => query.OrderByDescending(s => s.ScheduledAt)
        };

        return await query.ToDtosAsync(_clock, cancellationToken);
    }
}
