using FCQI.Application.Interfaces;
using FCQI.Domain;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Events;

/// <summary>
/// Notificaciones de BPMN 8, 11, 12 y 18: avisa por correo a la otra parte
/// de la cita. Si cambió el alumno, se entera el asesor, y al revés.
/// </summary>
public class SessionNotifier : ISessionObserver
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailSender _email;
    private readonly IClock _clock;

    public SessionNotifier(IApplicationDbContext context, IEmailSender email, IClock clock)
    {
        _context = context;
        _email = email;
        _clock = clock;
    }

    public async Task OnChangedAsync(SessionChanged change, CancellationToken cancellationToken)
    {
        var s = await _context.AdvisorySessions.AsNoTracking()
            .Where(x => x.Id == change.SessionId)
            .Select(x => new
            {
                x.StudentId,
                StudentEmail = x.Student.Person.Email,
                StudentName = x.Student.Person.DisplayName,
                AdvisorEmail = x.Advisor.Person.Email,
                AdvisorName = x.Advisor.Person.DisplayName,
                Subject = x.Subject.Name,
                x.ScheduledAt,
                x.MeetingLink
            })
            .SingleAsync(cancellationToken);

        var fromStudent = change.ChangedBy == s.StudentId;
        var (to, otherName) = fromStudent ? (s.AdvisorEmail, s.StudentName) : (s.StudentEmail, s.AdvisorName);
        var when = _clock.ToLocal(s.ScheduledAt).ToString("dddd dd/MM/yyyy HH:mm", Spanish);

        var what = change switch
        {
            { From: null } => "solicitó una nueva asesoría",
            { To: SessionStatusIds.Confirmed } => "confirmó la asesoría",
            { To: SessionStatusIds.Rejected } => "rechazó la asesoría",
            { To: SessionStatusIds.Cancelled } => "canceló la asesoría",
            { To: SessionStatusIds.Completed } => "registró la bitácora de la asesoría",
            _ => "actualizó la asesoría"
        };

        var body = $"{otherName} {what} de {s.Subject} del {when}.";
        if (s.MeetingLink is not null)
        {
            body += $"\nEnlace de la reunión: {s.MeetingLink}";
        }

        await _email.SendAsync(new EmailMessage(to, $"Asesoría de {s.Subject}: {what}", body), cancellationToken);
    }

    private static readonly System.Globalization.CultureInfo Spanish = new("es-MX");
}
