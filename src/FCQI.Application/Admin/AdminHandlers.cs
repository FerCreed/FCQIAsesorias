using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Domain;
using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Administration;

// Lista para dirección de todos los asesores, activos o no, con sus materias
// del ciclo vigente. A diferencia del catálogo del alumno, no filtra por
// estado para que dirección pueda reactivar o revisar a cualquiera.
public class GetAdminAdvisorsQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly CurrentTerm _term;

    // Recibe el contexto de datos y el resolvedor del ciclo vigente por inyección.
    public GetAdminAdvisorsQueryHandler(IApplicationDbContext context, CurrentTerm term)
    {
        _context = context;
        _term = term;
    }

    // Devuelve los asesores ordenados con los profesores primero y, dentro de
    // cada grupo, por apellido. Proyecta directo al DTO para que EF Core genere
    // una sola consulta sin cargar entidades completas.
    public async Task<IReadOnlyList<AdvisorDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var termId = await _term.IdAsync(cancellationToken);

        // AsNoTracking: consulta de solo lectura, no necesita seguimiento de cambios.
        // El primer OrderBy coloca a los asesores pares (alumnos) después de los profesores.
        return await _context.AdvisorProfiles
            .AsNoTracking()
            .OrderBy(a => a.Person.StudentProfile != null && a.Person.StudentProfile.IsActive ? 1 : 0)
            .ThenBy(a => a.Person.LastNamePaternal).ThenBy(a => a.Person.FirstName)
            .Select(a => new AdvisorDto(
                a.PersonId,
                a.Person.DisplayName,
                a.Person.Email,
                a.Program.Name,
                a.Person.StudentProfile != null && a.Person.StudentProfile.IsActive
                    ? AdvisorKinds.Student
                    : AdvisorKinds.Professor,
                a.DefaultModality.Name,
                a.IsActive,
                a.AdvisorSubjects
                    .Where(x => x.TermId == termId)
                    .Select(x => x.Subject.Name)
                    .OrderBy(n => n)
                    .ToList(),
                a.Person.StudentProfile != null && a.Person.StudentProfile.IsActive,
                null,
                0))
            .ToListAsync(cancellationToken);
    }
}

// Dirección reemplaza el conjunto de materias que un asesor imparte en el
// ciclo vigente. Aplica solo la diferencia (altas y bajas) en lugar de borrar
// y reinsertar todo, para no chocar con asesorías que ya referencian la
// asignación.
public class ReplaceAdvisorSubjectsCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly CurrentTerm _term;

    // Recibe el contexto de datos y el resolvedor del ciclo vigente por inyección.
    public ReplaceAdvisorSubjectsCommandHandler(IApplicationDbContext context, CurrentTerm term)
    {
        _context = context;
        _term = term;
    }

    // Deja al asesor con exactamente subjectIds en el ciclo
    // vigente. Lanza BusinessRuleException si el asesor o alguna
    // materia no existe, o si se intenta quitar una materia con asesorías.
    public async Task HandleAsync(int advisorId, IReadOnlyList<int> subjectIds, CancellationToken cancellationToken = default)
    {
        var termId = await _term.IdAsync(cancellationToken);

        var exists = await _context.AdvisorProfiles
            .AnyAsync(a => a.PersonId == advisorId, cancellationToken);
        if (!exists)
        {
            throw new BusinessRuleException("El asesor no existe.");
        }

        // HashSet: elimina duplicados del cliente y da búsquedas O(1) al calcular la diferencia.
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
                throw new BusinessRuleException(
                    "Hay materias que no existen o están dadas de baja: "
                    + string.Join(", ", unknown.OrderBy(id => id)) + ".");
            }
        }

        // Asignaciones actuales con seguimiento, porque algunas se van a borrar.
        var current = await _context.AdvisorSubjects
            .Where(x => x.TermId == termId && x.AdvisorId == advisorId)
            .ToListAsync(cancellationToken);

        // Lo que sobra respecto al conjunto pedido.
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
                throw new BusinessRuleException(
                    "No se pueden quitar materias con asesorías registradas en este ciclo: "
                    + string.Join(", ", inUse.OrderBy(n => n)) + ".");
            }

            _context.AdvisorSubjects.RemoveRange(toRemove);
        }

        // Alta solo de lo que falta; las asignaciones que ya existen conservan su fecha original.
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

        // Un solo SaveChanges: bajas y altas se aplican en la misma transacción.
        await _context.SaveChangesAsync(cancellationToken);
    }
}
