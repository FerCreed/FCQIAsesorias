using System.Text;
using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Queries;

/// <summary>
/// Sincronizar con calendario (BPMN 12): devuelve la cita como iCalendar
/// (RFC 5545), que importan Google Calendar, Outlook y Apple Calendar sin
/// credenciales ni integraciones de terceros.
/// </summary>
public class SessionCalendarQueryHandler
{
    private readonly IApplicationDbContext _context;

    public SessionCalendarQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<string> HandleAsync(int sessionId, Caller caller, CancellationToken cancellationToken = default)
    {
        var s = await _context.AdvisorySessions.AsNoTracking()
                    .Where(x => x.Id == sessionId)
                    .Select(x => new
                    {
                        Entity = x,
                        Subject = x.Subject.Name,
                        Advisor = x.Advisor.Person.DisplayName,
                        Location = x.Availability.Location.Name,
                        // Las horas se restan en memoria: MySQL devuelve la resta de TIME como entero.
                        x.Availability.StartTime,
                        x.Availability.EndTime
                    })
                    .SingleOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("La asesoría no existe.");

        SessionPolicy.RequireActor(s.Entity, caller);

        var start = DateTime.SpecifyKind(s.Entity.ScheduledAt, DateTimeKind.Utc);
        var where = s.Entity.MeetingLink ?? s.Location;

        return new StringBuilder()
            .Append("BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//FCQI UABC//Asesorias//ES\r\nBEGIN:VEVENT\r\n")
            .Append($"UID:asesoria-{s.Entity.Id}@fcqi.uabc.edu.mx\r\n")
            .Append($"DTSTAMP:{Stamp(DateTime.UtcNow)}\r\n")
            .Append($"DTSTART:{Stamp(start)}\r\n")
            .Append($"DTEND:{Stamp(start + (s.EndTime - s.StartTime))}\r\n")
            .Append($"SUMMARY:{Escape($"Asesoría de {s.Subject} con {s.Advisor}")}\r\n")
            .Append($"LOCATION:{Escape(where)}\r\n")
            .Append($"DESCRIPTION:{Escape(s.Entity.Topic)}\r\n")
            .Append("END:VEVENT\r\nEND:VCALENDAR\r\n")
            .ToString();
    }

    private static string Stamp(DateTime utc) => utc.ToString("yyyyMMdd'T'HHmmss'Z'");

    /// <summary>RFC 5545 §3.3.11: escapar \ ; , y saltos de línea en textos.</summary>
    internal static string Escape(string text) => text
        .Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,")
        .Replace("\r\n", "\\n").Replace("\n", "\\n");
}
