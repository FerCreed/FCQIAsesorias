using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Advisors.Queries;

/// <summary>Sedes activas, para el formulario de horarios del asesor.</summary>
public class GetLocationsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetLocationsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<LocationDto>> HandleAsync(CancellationToken cancellationToken = default)
        => await _context.Locations.AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .Select(l => new LocationDto(l.Id, l.Name, l.Modality.Name))
            .ToListAsync(cancellationToken);
}
