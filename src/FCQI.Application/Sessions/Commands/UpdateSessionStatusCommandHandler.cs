using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Domain;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Commands;

public class UpdateSessionStatusCommandHandler
{
    private readonly IApplicationDbContext _context;

    public UpdateSessionStatusCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task HandleAsync(int sessionId, string status, Caller caller, CancellationToken cancellationToken = default)
    {
        if (!SessionStatuses.All.Contains(status))
        {
            throw new InvalidOperationException("Estado no válido.");
        }

        var session = await _context.AdvisorySessions
                          .SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
                      ?? throw new InvalidOperationException("La solicitud no existe.");

        Authorize(session.StudentId, session.AdvisorId, status, caller);

        var target = SessionStatusIds.FromName(status);
        if (session.StatusId == target)
        {
            // Pedir el estado que ya tiene no es un error: la petición se
            // repitió, o dos pestañas hicieron lo mismo. No hay nada que hacer.
            return;
        }

        EnsureTransitionIsPossible(session.StatusId, target);

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
            // el cupo. Si saltan aquí, el mensaje debe poder enseñarse: antes
            // esta excepción subía sin tratar y la API respondía 500 con el
            // volcado de la excepción.
            throw new InvalidOperationException(
                "No se pudo cambiar el estado de la asesoría; es posible que " +
                "ese lugar ya esté ocupado. Vuelve a intentarlo.", ex);
        }
    }

    /// <summary>
    /// Cancelada y Rechazada son estados finales.
    ///
    /// Sin esta regla, confirmar una asesoría cancelada la devolvía a la vida:
    /// el trigger le reasignaba ActiveAt y volvía a ocupar el lugar. Si entre
    /// tanto otro alumno había tomado ese lugar, el índice único de cupo
    /// abortaba la operación y la API respondía 500. Y si el lugar seguía
    /// libre, era peor: una cita que el alumno canceló reaparecía como
    /// confirmada sin que él hiciera nada.
    /// </summary>
    private static void EnsureTransitionIsPossible(byte current, byte target)
    {
        var isFinal = current is SessionStatusIds.Cancelled or SessionStatusIds.Rejected;
        if (!isFinal)
        {
            return;
        }

        var name = current == SessionStatusIds.Cancelled
            ? SessionStatuses.Cancelled
            : SessionStatuses.Rejected;

        throw new InvalidOperationException(
            $"Esta asesoría está {name.ToLowerInvariant()} y ya no puede cambiar de estado. " +
            "Si se necesita la cita, hay que solicitarla de nuevo.");
    }

    /// <summary>
    /// Quién puede mover una asesoría, y a qué estado:
    ///   · Dirección: cualquier cambio.
    ///   · El asesor dueño: confirmar, rechazar o cancelar.
    ///   · El alumno dueño: solo cancelar la suya.
    ///   · Cualquier otro: nada.
    ///
    /// Los roles salen del token, no del selector de la interfaz: un asesor par
    /// que esté viendo la pantalla como alumno sigue siendo el asesor de sus
    /// propias asesorías.
    /// </summary>
    private static void Authorize(int studentId, int advisorId, string status, Caller caller)
    {
        if (caller.IsAdmin)
        {
            return;
        }

        var isOwningAdvisor = caller.IsAdvisor && caller.PersonId == advisorId;
        var isOwningStudent = caller.IsStudent && caller.PersonId == studentId;

        if (!isOwningAdvisor && !isOwningStudent)
        {
            throw new ForbiddenOperationException("Esta asesoría no es tuya.");
        }

        if (isOwningAdvisor)
        {
            return;
        }

        // Alumno dueño: solo puede darse de baja.
        if (status != SessionStatuses.Cancelled)
        {
            throw new ForbiddenOperationException(
                "Como alumno solo puedes cancelar tu asesoría; confirmarla o rechazarla le toca al asesor.");
        }
    }
}
