using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Advisors.Queries;

public class GetAvailabilitiesQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly CurrentTerm _term;

    public GetAvailabilitiesQueryHandler(IApplicationDbContext context, CurrentTerm term)
    {
        _context = context;
        _term = term;
    }

    public async Task<IReadOnlyList<AvailabilityDto>> HandleAsync(int advisorId, CancellationToken cancellationToken = default)
    {
        var termId = await _term.IdAsync(cancellationToken);

        // Modality ya no es columna de availabilities: la declara la sede.
        return await _context.Availabilities
            .AsNoTracking()
            .Where(a => a.AdvisorId == advisorId && a.TermId == termId && a.IsActive)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .Select(a => new AvailabilityDto(
                a.Id, a.AdvisorId, a.DayOfWeek, a.StartTime, a.EndTime,
                a.MaxCapacity, a.Location.Modality.Name, a.Location.Name))
            .ToListAsync(cancellationToken);
    }
}
