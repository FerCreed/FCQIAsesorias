using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Advisors.Queries;

public class GetAdvisorsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetAdvisorsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<AdvisorDto>> HandleAsync(int? subjectId, CancellationToken cancellationToken = default)
    {
        var query = _context.Advisors
            .AsNoTracking()
            .Where(a => a.IsActive)
            .Include(a => a.AdvisorSubjects)
            .ThenInclude(x => x.Subject)
            .AsQueryable();

        if (subjectId is not null)
        {
            query = query.Where(a => a.AdvisorSubjects.Any(x => x.SubjectId == subjectId));
        }

        return await query
            .OrderBy(a => a.FullName)
            .Select(a => new AdvisorDto(
                a.Id,
                a.FullName,
                a.Email,
                a.Area,
                a.DefaultModality,
                a.IsActive,
                a.AdvisorSubjects.Select(x => x.Subject.Name).OrderBy(n => n).ToList()))
            .ToListAsync(cancellationToken);
    }
}
