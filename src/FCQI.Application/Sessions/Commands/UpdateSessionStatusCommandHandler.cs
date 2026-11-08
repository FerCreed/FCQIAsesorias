using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Application.Sessions.Events;
using FCQI.Domain;
using FCQI.Domain.Entities;
using FCQI.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Commands;

/// <summary>
/// Confirmar, rechazar o cancelar una asesoría (BPMN 11, 12 y 18). Qué
/// transición es válida y quién puede hacerla lo decide <see cref="SessionState"/>;
/// aquí solo se orquesta: cargar, validar, guardar y avisar.
/// </summary>
public class UpdateSessionStatusCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IClock _clock;
    private readonly SessionPolicy _policy;
    private readonly SessionEventPublisher _events;

    public UpdateSessionStatusCommandHandler(
        IApplicationDbContext context, IClock clock, SessionPolicy policy, SessionEventPublisher events)
    {
        _context = context;
        _clock = clock;
        _policy = policy;
        _events = events;
    }

    public async Task HandleAsync(int sessionId, string status, Caller caller, CancellationToken cancellationToken = default)
    {
        if (!SessionStatuses.All.Contains(status))
        {
            throw new InvalidOperationException("Estado no válido.");
        }

        var session = await _context.AdvisorySessions
                          .SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
                      ?? throw new NotFoundException("La solicitud no existe.");

        var target = SessionStatusIds.FromName(status);
        if (session.StatusId == target)
        {
            // Pedir el estado que ya tiene no es un error: la petición se
            // repitió, o dos pestañas hicieron lo mismo. No hay nada que hacer.
            return;
        }

        var actor = EnsureAllowed(session, target, status, caller);
        if (actor == SessionActor.Student)
        {
            _policy.EnsureStudentCanChange(session.ScheduledAt, _clock);
        }

        await SaveAsync(session, target, caller, cancellationToken);
    }

    /// <summary>
    /// Valida la transición. Los estados finales van primero a propósito:
    /// confirmar una asesoría cancelada la devolvía a la vida y volvía a ocupar
    /// un lugar que quizá ya era de otro alumno.
    /// </summary>
    private static SessionActor EnsureAllowed(AdvisorySession session, byte target, string status, Caller caller)
    {
        var state = SessionState.From(session.StatusId);

        if (state.IsFinal)
        {
            throw new InvalidOperationException(
                $"Esta asesoría está {state.Name.ToLowerInvariant()} y ya no puede cambiar de estado. " +
                "Si se necesita la cita, hay que solicitarla de nuevo.");
        }

        if (target == SessionStatusIds.Completed)
        {
            throw new InvalidOperationException(
                "Para marcar la asesoría como realizada, registra su bitácora.");
        }

        var actor = SessionPolicy.RequireActor(session, caller);

        if (!state.CanMoveTo(target))
        {
            throw new InvalidOperationException(
                $"Una asesoría {state.Name.ToLowerInvariant()} no puede pasar a {status.ToLowerInvariant()}.");
        }

        if (!state.Allows(target, actor))
        {
            throw new ForbiddenOperationException(
                "Como alumno solo puedes cancelar tu asesoría; confirmarla o rechazarla le toca al asesor.");
        }

        return actor;
    }

    private async Task SaveAsync(AdvisorySession session, byte target, Caller caller, CancellationToken cancellationToken)
    {
        var previous = session.StatusId;

        // El trigger de la base recalcula ActivaEn (liberando el cupo cuando se
        // cancela) y registra el cambio en historial_estados_sesion.
        session.StatusId = target;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Los índices únicos sobre ActiveAt tienen la última palabra sobre
            // el cupo. Si saltan aquí, el mensaje debe poder enseñarse.
            throw new InvalidOperationException(
                "No se pudo cambiar el estado de la asesoría; es posible que " +
                "ese lugar ya esté ocupado. Vuelve a intentarlo.", ex);
        }

        await _events.PublishAsync(new SessionChanged(session.Id, previous, target, caller.PersonId), cancellationToken);
    }
}
