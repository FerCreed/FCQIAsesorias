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

        // El trigger de la base recalcula ActiveAt (liberando el cupo cuando se
        // cancela) y registra el cambio en session_status_history.
        session.StatusId = SessionStatusIds.FromName(status);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Quién puede mover una asesoría, y a qué estado:
    ///   · Dirección: cualquier cambio.
    ///   · El asesor dueño: confirmar, rechazar o cancelar.
    ///   · El alumno dueño: solo cancelar la suya.
    ///   · Cualquier otro: nada.
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
