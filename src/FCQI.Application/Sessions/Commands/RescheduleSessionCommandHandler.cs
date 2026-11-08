using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Application.Sessions.Dtos;
using FCQI.Application.Sessions.Events;
using FCQI.Domain;
using FCQI.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Commands;

/// <summary>
/// Reprogramar (BPMN 11): se solicita la cita nueva con el mismo asesor y
/// materia y, solo si se registró, se cancela la anterior. Reutiliza todas las
/// validaciones de <see cref="CreateSessionCommandHandler"/> (cupo, día, ciclo)
/// en vez de duplicarlas. La cita nueva vuelve a quedar Pendiente: el asesor
/// debe confirmar el nuevo horario.
/// </summary>
public class RescheduleSessionCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IClock _clock;
    private readonly SessionPolicy _policy;
    private readonly CreateSessionCommandHandler _create;
    private readonly SessionEventPublisher _events;

    public RescheduleSessionCommandHandler(
        IApplicationDbContext context, IClock clock, SessionPolicy policy,
        CreateSessionCommandHandler create, SessionEventPublisher events)
    {
        _context = context;
        _clock = clock;
        _policy = policy;
        _create = create;
        _events = events;
    }

    public async Task<SessionDto> HandleAsync(
        int sessionId, RescheduleSessionRequest request, Caller caller, CancellationToken cancellationToken = default)
    {
        var session = await _context.AdvisorySessions
                          .SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
                      ?? throw new NotFoundException("La asesoría no existe.");

        var actor = SessionPolicy.RequireActor(session, caller);
        if (actor == SessionActor.Advisor)
        {
            throw new ForbiddenOperationException("Reprogramar le toca al alumno; como asesor puedes cancelar.");
        }

        if (!SessionState.From(session.StatusId).OccupiesSeat)
        {
            throw new BusinessRuleException("Solo se pueden reprogramar asesorías pendientes o confirmadas.");
        }

        if (actor == SessionActor.Student)
        {
            _policy.EnsureStudentCanChange(session.ScheduledAt, _clock);
        }

        var replacement = await _create.HandleAsync(
            new CreateSessionRequest(session.StudentId, session.AdvisorId, session.SubjectId,
                request.AvailabilityId, request.ScheduledAt, session.Topic),
            caller, cancellationToken);

        var previous = session.StatusId;
        session.StatusId = SessionStatusIds.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);
        await _events.PublishAsync(
            new SessionChanged(session.Id, previous, SessionStatusIds.Cancelled, caller.PersonId), cancellationToken);

        return replacement;
    }
}
