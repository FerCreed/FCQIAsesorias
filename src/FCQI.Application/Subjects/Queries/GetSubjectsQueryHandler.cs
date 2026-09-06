using FCQI.Application.Interfaces;
using FCQI.Application.Subjects.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Subjects.Queries;

public class GetSubjectsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetSubjectsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SubjectDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Subjects
            .AsNoTracking()
            .OrderBy(s => s.Code)
            .Select(s => new SubjectDto(s.Id, s.Code, s.Name, s.Program))
            .ToListAsync(cancellationToken);
    }
}
