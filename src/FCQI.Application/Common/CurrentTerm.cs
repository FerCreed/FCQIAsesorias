using FCQI.Application.Interfaces;
using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Common;

/// <summary>
/// Resuelve el ciclo escolar vigente. El modelo anterior no tenía este
/// concepto: 'FCQI 2026-2' vivía repetido en cada materia.
/// </summary>
public class CurrentTerm
{
    private readonly IApplicationDbContext _context;

    public CurrentTerm(IApplicationDbContext context) => _context = context;

    public async Task<AcademicTermInfo> GetAsync(CancellationToken cancellationToken = default)
    {
        var term = await _context.Terms.AsNoTracking()
            .Where(t => t.IsCurrent)
            .OrderByDescending(t => t.StartsOn)
            .Select(t => new AcademicTermInfo(t.Id, t.Code, t.Name, t.StartsOn, t.EndsOn))
            .FirstOrDefaultAsync(cancellationToken);

        return term ?? throw new InvalidOperationException(
            "No hay un ciclo escolar marcado como vigente en ciclos_escolares.");
    }

    public async Task<short> IdAsync(CancellationToken cancellationToken = default)
        => (await GetAsync(cancellationToken)).Id;
}

public record AcademicTermInfo(short Id, string Code, string Name, DateOnly StartsOn, DateOnly EndsOn);
