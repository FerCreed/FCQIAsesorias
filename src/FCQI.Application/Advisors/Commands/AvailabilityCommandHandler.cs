using FCQI.Application.Advisors.Dtos;
using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Domain;
using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Advisors.Commands;

/// <summary>
/// El asesor administra sus bloques semanales del ciclo vigente:
/// configurar (BPMN 7), editar (BPMN 13) y retirar (BPMN 16).
/// </summary>
public class AvailabilityCommandHandler
{
    public const int MaxCapacityLimit = 30;

    private readonly IApplicationDbContext _context;
    private readonly CurrentTerm _term;
    private readonly IClock _clock;

    public AvailabilityCommandHandler(IApplicationDbContext context, CurrentTerm term, IClock clock)
    {
        _context = context;
        _term = term;
        _clock = clock;
    }

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

    public async Task UpdateAsync(int id, SaveAvailabilityRequest request, Caller caller, CancellationToken cancellationToken = default)
    {
        var block = await FindOwnAsync(id, caller, cancellationToken);
        await ValidateAsync(request, block.TermId, block.AdvisorId, exceptId: id, cancellationToken);

        var movesTime = block.DayOfWeek != request.DayOfWeek
                        || block.StartTime != request.StartTime
                        || block.EndTime != request.EndTime;
        var activeSeats = await ActiveSeatsAsync(id, cancellationToken);

        if (activeSeats > 0 && movesTime)
        {
            throw new InvalidOperationException(
                "Hay citas activas en este bloque; cancélalas o espera a que pasen antes de cambiar el día o la hora.");
        }

        if (request.MaxCapacity < activeSeats)
        {
            throw new InvalidOperationException($"El bloque ya tiene {activeSeats} lugares ocupados; el cupo no puede ser menor.");
        }

        Apply(block, request);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Baja lógica: las citas pasadas conservan su referencia al bloque.</summary>
    public async Task RetireAsync(int id, Caller caller, CancellationToken cancellationToken = default)
    {
        var block = await FindOwnAsync(id, caller, cancellationToken);

        if (await ActiveSeatsAsync(id, cancellationToken) > 0)
        {
            throw new InvalidOperationException("El bloque tiene citas activas; no se puede retirar todavía.");
        }

        block.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureAdvisor(Caller caller)
    {
        if (!caller.IsAdvisor)
        {
            throw new ForbiddenOperationException("Solo un asesor administra sus horarios.");
        }
    }

    private async Task<Availability> FindOwnAsync(int id, Caller caller, CancellationToken cancellationToken)
    {
        EnsureAdvisor(caller);
        var block = await _context.Availabilities.SingleOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken)
                    ?? throw new NotFoundException("El bloque de horario no existe.");

        return block.AdvisorId == caller.PersonId
            ? block
            : throw new ForbiddenOperationException("Ese bloque de horario no es tuyo.");
    }

    private async Task ValidateAsync(
        SaveAvailabilityRequest r, short termId, int advisorId, int? exceptId, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(r.DayOfWeek))
        {
            throw new InvalidOperationException("Día de la semana no válido.");
        }

        if (r.EndTime <= r.StartTime)
        {
            throw new InvalidOperationException("La hora de fin debe ser posterior a la de inicio.");
        }

        if (r.MaxCapacity is < 1 or > MaxCapacityLimit)
        {
            throw new InvalidOperationException($"El cupo debe estar entre 1 y {MaxCapacityLimit}.");
        }

        if (!await _context.Locations.AnyAsync(l => l.Id == r.LocationId && l.IsActive, cancellationToken))
        {
            throw new InvalidOperationException("El lugar elegido no existe.");
        }

        var overlaps = await _context.Availabilities.AnyAsync(a =>
            a.TermId == termId && a.AdvisorId == advisorId && a.IsActive && a.Id != exceptId &&
            a.DayOfWeek == r.DayOfWeek && a.StartTime < r.EndTime && r.StartTime < a.EndTime, cancellationToken);
        if (overlaps)
        {
            throw new InvalidOperationException("Ese horario se traslapa con otro de tus bloques.");
        }
    }

    /// <summary>Citas por venir que todavía ocupan lugar en el bloque.</summary>
    private Task<int> ActiveSeatsAsync(int availabilityId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        return _context.AdvisorySessions.CountAsync(s =>
            s.AvailabilityId == availabilityId && s.ScheduledAt > now &&
            (s.StatusId == SessionStatusIds.Pending || s.StatusId == SessionStatusIds.Confirmed), cancellationToken);
    }

    private static void Apply(Availability block, SaveAvailabilityRequest r)
    {
        block.DayOfWeek = r.DayOfWeek;
        block.StartTime = r.StartTime;
        block.EndTime = r.EndTime;
        block.MaxCapacity = r.MaxCapacity;
        block.LocationId = r.LocationId;
    }
}
