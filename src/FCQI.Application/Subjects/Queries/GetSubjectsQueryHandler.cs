using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Application.Subjects.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Subjects.Queries;

public class GetSubjectsQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly CurrentTerm _term;

    public GetSubjectsQueryHandler(IApplicationDbContext context, CurrentTerm term)
    {
        _context = context;
        _term = term;
    }

    public async Task<IReadOnlyList<SubjectDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        // subjects ya no tiene columna Program: su contenido ('FCQI 2026-2')
        // era en realidad el ciclo escolar. Se toma de academic_terms para no
        // cambiar el contrato que consume la interfaz.
        var term = await _term.GetAsync(cancellationToken);
        var program = $"FCQI {term.Code}";

        return await _context.Subjects
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Code)
            .Select(s => new SubjectDto(s.Id, s.Code, s.Name, program))
            .ToListAsync(cancellationToken);
    }
}
