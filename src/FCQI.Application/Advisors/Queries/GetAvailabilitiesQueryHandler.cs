using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Advisors.Queries;

public class GetAvailabilitiesQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetAvailabilitiesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<AvailabilityDto>> HandleAsync(int advisorId, CancellationToken cancellationToken = default)
    {
        return await _context.Availabilities
            .AsNoTracking()
            .Where(a => a.AdvisorId == advisorId)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .Select(a => new AvailabilityDto(a.Id, a.AdvisorId, a.DayOfWeek, a.StartTime, a.EndTime, a.MaxCapacity, a.Modality, a.Location))
            .ToListAsync(cancellationToken);
    }
}
