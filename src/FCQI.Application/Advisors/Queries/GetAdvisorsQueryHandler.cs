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

        var rows = await query
            .OrderBy(a => a.Person.StudentProfile != null && a.Person.StudentProfile.IsActive ? 1 : 0)
            .ThenBy(a => a.Person.LastNamePaternal).ThenBy(a => a.Person.FirstName)
            .Select(a => new
            {
                a.PersonId,
                a.Person.DisplayName,
                a.Person.Email,
                Area = a.Program.Name,
                IsStudent = a.Person.StudentProfile != null && a.Person.StudentProfile.IsActive,
                Modality = a.DefaultModality.Name,
                a.IsActive,
                Subjects = a.AdvisorSubjects
                    .Where(x => x.TermId == termId)
                    .Select(x => x.Subject.Name)
                    .OrderBy(n => n)
                    .ToList(),
                Blocks = a.Availabilities
                    .Where(b => b.TermId == termId && b.IsActive)
                    .Select(b => new { b.DayOfWeek, b.StartTime, b.EndTime })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        // Las horas se suman en memoria: MySQL devuelve la resta de TIME como entero.
        return rows.Select(a => new AdvisorDto(
                a.PersonId, a.DisplayName, a.Email, a.Area,
                a.IsStudent ? AdvisorKinds.Student : AdvisorKinds.Professor,
                a.Modality, a.IsActive, a.Subjects, a.IsStudent,
                a.Blocks.Select(b => (int)b.DayOfWeek).Distinct().Order().ToList(),
                Math.Round(a.Blocks.Sum(b => (b.EndTime - b.StartTime).TotalHours), 1)))
            .ToList();
    }
}
