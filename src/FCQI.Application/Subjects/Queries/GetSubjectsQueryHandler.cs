using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Application.Subjects.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Subjects.Queries;

// Catálogo de materias activas con sus carreras y el número de asesores que
// las imparten en el ciclo vigente.
public class GetSubjectsQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly CurrentTerm _term;

    // Recibe el contexto de datos y el resolvedor del ciclo vigente por inyección.
    public GetSubjectsQueryHandler(IApplicationDbContext context, CurrentTerm term)
    {
        _context = context;
        _term = term;
    }

    // Devuelve las materias activas ordenadas por nombre.
    public async Task<IReadOnlyList<SubjectDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        // materias ya no tiene columna Program: su contenido ('FCQI 2026-2')
        // era en realidad el ciclo escolar. Se toma de ciclos_escolares para no
        // cambiar el contrato que consume la interfaz.
        var term = await _term.GetAsync(cancellationToken);
        var program = $"FCQI {term.Code}";

        // AdvisorCount solo cuenta asesores activos, para no ofrecer materias que nadie atiende.
        return await _context.Subjects
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SubjectDto(
                s.Id, s.Code, s.Name, program,
                s.ProgramSubjects.Select(p => p.Program.Name).OrderBy(n => n).ToList(),
                s.AdvisorSubjects.Count(a => a.TermId == term.Id && a.Advisor.IsActive)))
            .ToListAsync(cancellationToken);
    }
}
