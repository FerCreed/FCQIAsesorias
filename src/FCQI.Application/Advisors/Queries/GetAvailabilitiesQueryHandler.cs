using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Advisors.Queries;

// Bloques de horario activos de un asesor en el ciclo vigente, ordenados por
// día y hora para pintarlos tal cual en el calendario.
public class GetAvailabilitiesQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly CurrentTerm _term;

    // Recibe el contexto de datos y el resolvedor del ciclo vigente por inyección.
    public GetAvailabilitiesQueryHandler(IApplicationDbContext context, CurrentTerm term)
    {
        _context = context;
        _term = term;
    }

    // Devuelve los bloques del asesor indicado; lista vacía si no tiene.
    public async Task<IReadOnlyList<AvailabilityDto>> HandleAsync(int advisorId, CancellationToken cancellationToken = default)
    {
        var termId = await _term.IdAsync(cancellationToken);

        // La modalidad ya no es columna de horarios: la declara la sede.
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
