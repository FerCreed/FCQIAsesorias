using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Interfaces;
using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Administration;

public class GetAdminAdvisorsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetAdminAdvisorsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<AdvisorDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Advisors
            .AsNoTracking()
            .Include(a => a.AdvisorSubjects)
            .ThenInclude(x => x.Subject)
            .OrderBy(a => a.FullName)
            .Select(a => new AdvisorDto(
                a.Id, a.FullName, a.Email, a.Area, a.DefaultModality, a.IsActive,
                a.AdvisorSubjects.Select(x => x.Subject.Name).OrderBy(n => n).ToList()))
            .ToListAsync(cancellationToken);
    }
}

public class ReplaceAdvisorSubjectsCommandHandler
{
    private readonly IApplicationDbContext _context;

    public ReplaceAdvisorSubjectsCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task HandleAsync(int advisorId, IReadOnlyList<int> subjectIds, CancellationToken cancellationToken = default)
    {
        var advisor = await _context.Advisors
                          .Include(a => a.AdvisorSubjects)
                          .SingleOrDefaultAsync(a => a.Id == advisorId, cancellationToken)
                      ?? throw new InvalidOperationException("El tutor no existe.");

        advisor.AdvisorSubjects.Clear();
        foreach (var subjectId in subjectIds.Distinct())
        {
            advisor.AdvisorSubjects.Add(new AdvisorSubject { AdvisorId = advisorId, SubjectId = subjectId });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
