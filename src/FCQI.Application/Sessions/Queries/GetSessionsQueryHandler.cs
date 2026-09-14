using FCQI.Application.Interfaces;
using FCQI.Application.Sessions.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Queries;

public class GetSessionsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetSessionsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<SessionDto>> HandleAsync(int? studentId, int? advisorId, string? status, CancellationToken cancellationToken = default)
    {
        var query = _context.AdvisorySessions
            .AsNoTracking()
            .Include(s => s.Student)
            .Include(s => s.Advisor)
            .Include(s => s.Subject)
            .AsQueryable();

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
            query = query.Where(s => s.Status == status);
        }

        return await query
            .OrderByDescending(s => s.ScheduledAt)
            .Select(s => new SessionDto(
                s.Id, s.StudentId, s.Student.FullName,
                s.AdvisorId, s.Advisor.FullName,
                s.SubjectId, s.Subject.Name,
                s.AvailabilityId, s.ScheduledAt, s.Topic, s.Status))
            .ToListAsync(cancellationToken);
    }
}
