using FCQI.Application.Interfaces;
using FCQI.Domain;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Events;

/// <summary>
/// Notificaciones de BPMN 8, 11, 12 y 18: avisa por correo a la otra parte
/// de la cita. Si cambió el alumno, se entera el asesor, y al revés; si fue
/// dirección, se enteran los dos. El correo nombra siempre a quien actuó.
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
                x.AdvisorId,
                StudentEmail = x.Student.Person.Email,
                StudentName = x.Student.Person.DisplayName,
                AdvisorEmail = x.Advisor.Person.Email,
                AdvisorName = x.Advisor.Person.DisplayName,
                Subject = x.Subject.Name,
                x.ScheduledAt,
                x.MeetingLink
            })
            .SingleAsync(cancellationToken);

        var actor = await _context.People.AsNoTracking()
            .Where(p => p.Id == change.ChangedBy)
            .Select(p => p.DisplayName)
            .SingleOrDefaultAsync(cancellationToken) ?? "Dirección del programa";

        string[] recipients = change.ChangedBy == s.StudentId ? [s.AdvisorEmail]
            : change.ChangedBy == s.AdvisorId ? [s.StudentEmail]
            : [s.StudentEmail, s.AdvisorEmail];

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

        var body = $"{actor} {what} de {s.Subject} entre {s.StudentName} y {s.AdvisorName}, del {when}.";
        if (s.MeetingLink is not null)
        {
            body += $"\nEnlace de la reunión: {s.MeetingLink}";
        }

        foreach (var to in recipients)
        {
            await _email.SendAsync(new EmailMessage(to, $"Asesoría de {s.Subject}: {what}", body), cancellationToken);
        }
    }

    private static readonly System.Globalization.CultureInfo Spanish = new("es-MX");
}
