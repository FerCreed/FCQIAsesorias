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

    public async Task<IReadOnlyList<SessionDto>> HandleAsync(
        int? studentId, int? advisorId, string? status, Caller caller,
        CancellationToken cancellationToken = default)
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

            if (studentId is not null && studentId != caller.PersonId)
            {
                throw new ForbiddenOperationException("Solo puedes consultar tus propias asesorías.");
            }

            if (advisorId is not null && advisorId != caller.PersonId)
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

        var rows = await query
            .OrderByDescending(s => s.ScheduledAt)
            .Select(s => new
            {
                s.Id,
                s.StudentId,
                StudentName = s.Student.Person.DisplayName,
                s.AdvisorId,
                AdvisorName = s.Advisor.Person.DisplayName,
                s.SubjectId,
                SubjectName = s.Subject.Name,
                s.AvailabilityId,
                s.ScheduledAt,
                s.Topic,
                Status = s.Status.Name
            })
            .ToListAsync(cancellationToken);

        // La base guarda UTC; la conversión a hora local se hace aquí, fuera
        // de la consulta, porque TimeZoneInfo no se traduce a SQL.
        return rows.Select(r => new SessionDto(
            (int)r.Id, r.StudentId, r.StudentName,
            r.AdvisorId, r.AdvisorName,
            r.SubjectId, r.SubjectName,
            r.AvailabilityId,
            _clock.ToLocal(r.ScheduledAt),
            DateTime.SpecifyKind(r.ScheduledAt, DateTimeKind.Utc),
            r.Topic, r.Status)).ToList();
    }
}
