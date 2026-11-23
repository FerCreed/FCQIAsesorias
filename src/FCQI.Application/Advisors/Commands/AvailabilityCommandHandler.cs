using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Domain;
using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Advisors.Commands;

// El asesor administra sus bloques semanales del ciclo vigente:
// configurar (BPMN 7), editar (BPMN 13) y retirar (BPMN 16).
public class AvailabilityCommandHandler
{
    // Cupo máximo por bloque; tope razonable para una asesoría grupal en un salón.
    public const int MaxCapacityLimit = 30;

    private readonly IApplicationDbContext _context;
    private readonly CurrentTerm _term;
    private readonly IClock _clock;

    // Recibe el contexto de datos, el ciclo vigente y el reloj por inyección;
    // el reloj es una abstracción para poder fijar "ahora" en las pruebas.
    public AvailabilityCommandHandler(IApplicationDbContext context, CurrentTerm term, IClock clock)
    {
        _context = context;
        _term = term;
        _clock = clock;
    }

    // Da de alta un bloque semanal del asesor en el ciclo vigente (BPMN 7) y
    // devuelve su identificador.
    public async Task<int> CreateAsync(SaveAvailabilityRequest request, Caller caller, CancellationToken cancellationToken = default)
    {
        EnsureAdvisor(caller);
        var termId = await _term.IdAsync(cancellationToken);
        await ValidateAsync(request, termId, caller.PersonId, exceptId: null, cancellationToken);

        // El índice único (ciclo, asesor, día, hora) incluye los bloques
        // retirados: si existe uno igual dado de baja, se reactiva en vez de
        // chocar con la restricción.
        var block = await _context.Availabilities.SingleOrDefaultAsync(a =>
                        a.TermId == termId && a.AdvisorId == caller.PersonId &&
                        a.DayOfWeek == request.DayOfWeek && a.StartTime == request.StartTime, cancellationToken)
                    ?? _context.Availabilities.Add(new Availability { TermId = termId, AdvisorId = caller.PersonId }).Entity;

        Apply(block, request);
        block.IsActive = true;
        await _context.SaveChangesAsync(cancellationToken);
        return block.Id;
    }

    // Edita un bloque propio (BPMN 13). Con citas activas solo permite cambiar
    // cupo o sede: mover el día o la hora dejaría a los alumnos con una cita
    // en un horario que ya no existe.
    public async Task UpdateAsync(int id, SaveAvailabilityRequest request, Caller caller, CancellationToken cancellationToken = default)
    {
        var block = await FindOwnAsync(id, caller, cancellationToken);
        // exceptId excluye al propio bloque de la búsqueda de traslapes.
        await ValidateAsync(request, block.TermId, block.AdvisorId, exceptId: id, cancellationToken);

        // Cualquier cambio de día u horario afecta a las citas ya agendadas.
        var movesTime = block.DayOfWeek != request.DayOfWeek
                        || block.StartTime != request.StartTime
                        || block.EndTime != request.EndTime;
        var activeSeats = await ActiveSeatsAsync(id, cancellationToken);

        if (activeSeats > 0 && movesTime)
        {
            throw new BusinessRuleException(
                "Hay citas activas en este bloque; cancélalas o espera a que pasen antes de cambiar el día o la hora.");
        }

        // Reducir el cupo por debajo de lo ocupado dejaría citas sin lugar.
        if (request.MaxCapacity < activeSeats)
        {
            throw new BusinessRuleException($"El bloque ya tiene {activeSeats} lugares ocupados; el cupo no puede ser menor.");
        }

        Apply(block, request);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Baja lógica: las citas pasadas conservan su referencia al bloque.
    public async Task RetireAsync(int id, Caller caller, CancellationToken cancellationToken = default)
    {
        var block = await FindOwnAsync(id, caller, cancellationToken);

        if (await ActiveSeatsAsync(id, cancellationToken) > 0)
        {
            throw new BusinessRuleException("El bloque tiene citas activas; no se puede retirar todavía.");
        }

        block.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Rechaza con 403 a quien no tenga rol de asesor.
    private static void EnsureAdvisor(Caller caller)
    {
        if (!caller.IsAdvisor)
        {
            throw new ForbiddenOperationException("Solo un asesor administra sus horarios.");
        }
    }

    // Carga un bloque activo y comprueba que pertenezca a quien llama. Evita
    // que un asesor edite horarios ajenos adivinando identificadores.
    private async Task<Availability> FindOwnAsync(int id, Caller caller, CancellationToken cancellationToken)
    {
        EnsureAdvisor(caller);
        var block = await _context.Availabilities.SingleOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken)
                    ?? throw new NotFoundException("El bloque de horario no existe.");

        return block.AdvisorId == caller.PersonId
            ? block
            : throw new ForbiddenOperationException("Ese bloque de horario no es tuyo.");
    }

    // Reglas comunes de alta y edición: día válido, horario coherente, cupo en
    // rango, sede vigente y sin traslape con otros bloques del mismo asesor.
    // Se validan aquí con mensajes claros en vez de dejar que la base rechace
    // la fila con un error de restricción.
    private async Task ValidateAsync(
        SaveAvailabilityRequest r, short termId, int advisorId, int? exceptId, CancellationToken cancellationToken)
    {
        // El enum llega deserializado de JSON y puede traer cualquier entero.
        if (!Enum.IsDefined(r.DayOfWeek))
        {
            throw new BusinessRuleException("Día de la semana no válido.");
        }

        if (r.EndTime <= r.StartTime)
        {
            throw new BusinessRuleException("La hora de fin debe ser posterior a la de inicio.");
        }

        if (r.MaxCapacity is < 1 or > MaxCapacityLimit)
        {
            throw new BusinessRuleException($"El cupo debe estar entre 1 y {MaxCapacityLimit}.");
        }

        if (!await _context.Locations.AnyAsync(l => l.Id == r.LocationId && l.IsActive, cancellationToken))
        {
            throw new BusinessRuleException("El lugar elegido no existe.");
        }

        // Dos intervalos se traslapan cuando cada uno empieza antes de que termine el otro.
        var overlaps = await _context.Availabilities.AnyAsync(a =>
            a.TermId == termId && a.AdvisorId == advisorId && a.IsActive && a.Id != exceptId &&
            a.DayOfWeek == r.DayOfWeek && a.StartTime < r.EndTime && r.StartTime < a.EndTime, cancellationToken);
        if (overlaps)
        {
            throw new BusinessRuleException("Ese horario se traslapa con otro de tus bloques.");
        }
    }

    // Citas por venir que todavía ocupan lugar en el bloque.
    private Task<int> ActiveSeatsAsync(int availabilityId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        return _context.AdvisorySessions.CountAsync(s =>
            s.AvailabilityId == availabilityId && s.ScheduledAt > now &&
            (s.StatusId == SessionStatusIds.Pending || s.StatusId == SessionStatusIds.Confirmed), cancellationToken);
    }

    // Copia los campos editables de la petición al bloque; compartido por alta y edición.
    private static void Apply(Availability block, SaveAvailabilityRequest r)
    {
        block.DayOfWeek = r.DayOfWeek;
        block.StartTime = r.StartTime;
        block.EndTime = r.EndTime;
        block.MaxCapacity = r.MaxCapacity;
        block.LocationId = r.LocationId;
    }
}
