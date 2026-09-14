using FCQI.Application.Interfaces;
using FCQI.Application.Sessions.Dtos;
using FCQI.Domain;
using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Commands;

public class CreateSessionCommandHandler
{
    private readonly IApplicationDbContext _context;

    public CreateSessionCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<SessionDto> HandleAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var teaches = await _context.AdvisorSubjects.AnyAsync(
            x => x.AdvisorId == request.AdvisorId && x.SubjectId == request.SubjectId, cancellationToken);
        if (!teaches)
        {
            throw new InvalidOperationException("El directivo no ha asignado esta materia a ese tutor.");
        }

        var slot = await _context.Availabilities.SingleOrDefaultAsync(a => a.Id == request.AvailabilityId, cancellationToken)
                   ?? throw new InvalidOperationException("El horario no existe.");
        if (slot.AdvisorId != request.AdvisorId)
        {
            throw new InvalidOperationException("El horario no pertenece a ese tutor.");
        }

        var overlap = await _context.AdvisorySessions.AnyAsync(s =>
            s.StudentId == request.StudentId
            && s.AvailabilityId == request.AvailabilityId
            && s.ScheduledAt.Date == request.ScheduledAt.Date
            && (s.Status == SessionStatuses.Pending || s.Status == SessionStatuses.Confirmed), cancellationToken);
        if (overlap)
        {
            throw new InvalidOperationException("Ya tienes una solicitud activa en ese horario.");
        }

        var session = new AdvisorySession
        {
            StudentId = request.StudentId,
            AdvisorId = request.AdvisorId,
            SubjectId = request.SubjectId,
            AvailabilityId = request.AvailabilityId,
            ScheduledAt = request.ScheduledAt,
            Topic = string.IsNullOrWhiteSpace(request.Topic) ? "Asesoría" : request.Topic.Trim(),
            Status = SessionStatuses.Pending
        };

        _context.AdvisorySessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return await MapAsync(session.Id, cancellationToken);
    }

    private async Task<SessionDto> MapAsync(int id, CancellationToken cancellationToken)
    {
        var session = await _context.AdvisorySessions
            .AsNoTracking()
            .Include(s => s.Student)
            .Include(s => s.Advisor)
            .Include(s => s.Subject)
            .SingleAsync(s => s.Id == id, cancellationToken);

        return new SessionDto(
            session.Id, session.StudentId, session.Student.FullName,
            session.AdvisorId, session.Advisor.FullName,
            session.SubjectId, session.Subject.Name,
            session.AvailabilityId, session.ScheduledAt, session.Topic, session.Status);
    }
}
