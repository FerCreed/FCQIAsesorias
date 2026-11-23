using FCQI.Application.Interfaces;
using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Common;

// Resuelve el ciclo escolar vigente. El modelo anterior no tenía este
// concepto: 'FCQI 2026-2' vivía repetido en cada materia.
public class CurrentTerm
{
    private readonly IApplicationDbContext _context;

    // Recibe el contexto de datos por inyección.
    public CurrentTerm(IApplicationDbContext context) => _context = context;

    // Devuelve el ciclo vigente. Lanza BusinessRuleException si
    // no hay ninguno, porque sin ciclo no se pueden publicar horarios ni agendar.
    public async Task<AcademicTermInfo> GetAsync(CancellationToken cancellationToken = default)
    {
        // Si por error hubiera más de un ciclo vigente, gana el más reciente en vez de fallar.
        var term = await _context.Terms.AsNoTracking()
            .Where(t => t.IsCurrent)
            .OrderByDescending(t => t.StartsOn)
            .Select(t => new AcademicTermInfo(t.Id, t.Code, t.Name, t.StartsOn, t.EndsOn))
            .FirstOrDefaultAsync(cancellationToken);

        return term ?? throw new BusinessRuleException(
            "No hay un ciclo escolar marcado como vigente en ciclos_escolares.");
    }

    // Atajo para los casos de uso que solo necesitan el identificador del ciclo.
    public async Task<short> IdAsync(CancellationToken cancellationToken = default)
        => (await GetAsync(cancellationToken)).Id;
}

// Vista inmutable del ciclo escolar, desacoplada de la entidad de EF Core.
public record AcademicTermInfo(short Id, string Code, string Name, DateOnly StartsOn, DateOnly EndsOn);
