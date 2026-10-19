using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Domain;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Advisors.Queries;

public class GetAdvisorsQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly CurrentTerm _term;

    public GetAdvisorsQueryHandler(IApplicationDbContext context, CurrentTerm term)
    {
        _context = context;
        _term = term;
    }

    public async Task<IReadOnlyList<AdvisorDto>> HandleAsync(int? subjectId, CancellationToken cancellationToken = default)
    {
        var termId = await _term.IdAsync(cancellationToken);

        var query = _context.AdvisorProfiles
            .AsNoTracking()
            .Where(a => a.IsActive && a.Person.IsActive);

        if (subjectId is not null)
        {
            query = query.Where(a => a.AdvisorSubjects
                .Any(x => x.SubjectId == subjectId && x.TermId == termId));
        }

        return await query
            .OrderBy(a => a.Person.StudentProfile != null && a.Person.StudentProfile.IsActive ? 1 : 0)
            .ThenBy(a => a.Person.LastNamePaternal).ThenBy(a => a.Person.FirstName)
            .Select(a => new AdvisorDto(
                a.PersonId,
                a.Person.DisplayName,
                a.Person.Email,
                a.Program.Name,
                a.Person.StudentProfile != null && a.Person.StudentProfile.IsActive
                    ? TutorKinds.Student
                    : TutorKinds.Professor,
                a.DefaultModality.Name,
                a.IsActive,
                a.AdvisorSubjects
                    .Where(x => x.TermId == termId)
                    .Select(x => x.Subject.Name)
                    .OrderBy(n => n)
                    .ToList()))
            .ToListAsync(cancellationToken);
    }
}
