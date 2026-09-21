using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Administration;

public class GetAdminAdvisorsQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly CurrentTerm _term;

    public GetAdminAdvisorsQueryHandler(IApplicationDbContext context, CurrentTerm term)
    {
        _context = context;
        _term = term;
    }

    public async Task<IReadOnlyList<AdvisorDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var termId = await _term.IdAsync(cancellationToken);

        return await _context.AdvisorProfiles
            .AsNoTracking()
            .OrderBy(a => a.Person.LastNamePaternal).ThenBy(a => a.Person.FirstName)
            .Select(a => new AdvisorDto(
                a.PersonId,
                a.Person.DisplayName,
                a.Person.Email,
                a.Program.Name,
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

public class ReplaceAdvisorSubjectsCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly CurrentTerm _term;

    public ReplaceAdvisorSubjectsCommandHandler(IApplicationDbContext context, CurrentTerm term)
    {
        _context = context;
        _term = term;
    }

    public async Task HandleAsync(int advisorId, IReadOnlyList<int> subjectIds, CancellationToken cancellationToken = default)
    {
        var termId = await _term.IdAsync(cancellationToken);

        var exists = await _context.AdvisorProfiles
            .AnyAsync(a => a.PersonId == advisorId, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException("El tutor no existe.");
        }

        var wanted = subjectIds.Distinct().ToHashSet();

        // Las materias tienen que existir y estar vigentes. Sin esta
        // comprobación, un id inventado llegaba hasta el INSERT y la llave
        // foránea lo rechazaba con un 500 y un volcado de excepción; y una
        // materia dada de baja podía asignarse aunque el alumno no la vea
        // nunca en el catálogo.
        if (wanted.Count > 0)
        {
            var valid = await _context.Subjects
                .Where(s => wanted.Contains(s.Id) && s.IsActive)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            var unknown = wanted.Except(valid).ToList();
            if (unknown.Count > 0)
            {
                throw new InvalidOperationException(
                    "Hay materias que no existen o están dadas de baja: "
                    + string.Join(", ", unknown.OrderBy(id => id)) + ".");
            }
        }

        var current = await _context.AdvisorSubjects
            .Where(x => x.TermId == termId && x.AdvisorId == advisorId)
            .ToListAsync(cancellationToken);

        var toRemove = current.Where(x => !wanted.Contains(x.SubjectId)).ToList();

        // Antes este método borraba TODAS las asignaciones y las reinsertaba.
        // Con el modelo nuevo las sesiones referencian (ciclo, asesor, materia)
        // por llave foránea, así que borrar una materia con asesorías vivas
        // fallaría con un error de restricción. Se comprueba primero y se
        // devuelve un mensaje que explique el porqué.
        if (toRemove.Count > 0)
        {
            var removingIds = toRemove.Select(x => x.SubjectId).ToList();

            var inUse = await _context.AdvisorySessions
                .Where(s => s.TermId == termId
                            && s.AdvisorId == advisorId
                            && removingIds.Contains(s.SubjectId))
                .Select(s => s.Subject.Name)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (inUse.Count > 0)
            {
                throw new InvalidOperationException(
                    "No se pueden quitar materias con asesorías registradas en este ciclo: "
                    + string.Join(", ", inUse.OrderBy(n => n)) + ".");
            }

            _context.AdvisorSubjects.RemoveRange(toRemove);
        }

        var currentIds = current.Select(x => x.SubjectId).ToHashSet();
        foreach (var subjectId in wanted.Where(id => !currentIds.Contains(id)))
        {
            _context.AdvisorSubjects.Add(new AdvisorSubject
            {
                TermId = termId,
                AdvisorId = advisorId,
                SubjectId = subjectId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
