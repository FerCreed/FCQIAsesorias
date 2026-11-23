using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Application.Sessions.Dtos;
using FCQI.Application.Sessions.Events;
using FCQI.Domain;
using FCQI.Domain.Entities;
using FCQI.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Commands;

// Registrar bitácora (BPMN 9): el asesor pasa lista y anota el avance; la
// asesoría queda Realizada. Solo para citas confirmadas que ya empezaron.
public class RecordSessionLogCommandHandler
{
    // Longitud máxima de las notas, alineada con la columna de la base.
    public const int MaxNotesLength = 2000;

    private readonly IApplicationDbContext _context;
    private readonly IClock _clock;
    private readonly SessionEventPublisher _events;

    // Recibe el contexto de datos, el reloj y el publicador de eventos por inyección.
    public RecordSessionLogCommandHandler(IApplicationDbContext context, IClock clock, SessionEventPublisher events)
    {
        _context = context;
        _clock = clock;
        _events = events;
    }

    // Guarda la bitácora y marca la asesoría como Realizada en la misma
    // operación, para que nunca exista una realizada sin bitácora.
    public async Task HandleAsync(int sessionId, SessionLogRequest request, Caller caller, CancellationToken cancellationToken = default)
    {
        // Con seguimiento: se modificará el estado de la asesoría.
        var session = await _context.AdvisorySessions
                          .SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
                      ?? throw new NotFoundException("La asesoría no existe.");

        // El patrón Estado decide si la transición existe y si este actor puede hacerla.
        var actor = SessionPolicy.RequireActor(session, caller);
        var state = SessionState.From(session.StatusId);

        if (!state.CanMoveTo(SessionStatusIds.Completed))
        {
            throw new BusinessRuleException("Solo se registra bitácora de asesorías confirmadas.");
        }

        if (!state.Allows(SessionStatusIds.Completed, actor))
        {
            throw new ForbiddenOperationException("La bitácora la registra el asesor de la cita.");
        }

        if (session.ScheduledAt > _clock.UtcNow)
        {
            throw new BusinessRuleException("La asesoría todavía no empieza; registra la bitácora al impartirla.");
        }

        var notes = request.Notes?.Trim();
        if (notes?.Length > MaxNotesLength)
        {
            throw new BusinessRuleException($"Las notas admiten hasta {MaxNotesLength} caracteres.");
        }

        // Notas vacías se guardan como NULL para distinguir "sin notas" de texto en blanco.
        _context.SessionLogs.Add(new SessionLog
        {
            SessionId = session.Id,
            Attended = request.Attended,
            Notes = string.IsNullOrEmpty(notes) ? null : notes,
            RecordedBy = caller.PersonId,
            RecordedAt = _clock.UtcNow
        });
        session.StatusId = SessionStatusIds.Completed;

        // Se publica después de guardar: los observadores solo reaccionan a cambios ya persistidos.
        await _context.SaveChangesAsync(cancellationToken);
        await _events.PublishAsync(
            new SessionChanged(session.Id, SessionStatusIds.Confirmed, SessionStatusIds.Completed, caller.PersonId),
            cancellationToken);
    }
}
