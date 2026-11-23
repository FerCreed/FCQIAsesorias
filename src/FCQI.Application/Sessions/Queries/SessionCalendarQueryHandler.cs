using System.Text;
using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Queries;

// Sincronizar con calendario (BPMN 12): devuelve la cita como iCalendar
// (RFC 5545), que importan Google Calendar, Outlook y Apple Calendar sin
// credenciales ni integraciones de terceros.
public class SessionCalendarQueryHandler
{
    private readonly IApplicationDbContext _context;

    // Recibe el contexto de datos por inyección.
    public SessionCalendarQueryHandler(IApplicationDbContext context) => _context = context;

    // Devuelve el texto .ics de la asesoría. Solo sus participantes y
    // dirección pueden descargarlo.
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

        // EF Core devuelve DateTime sin Kind; se marca como UTC para emitir horas con sufijo Z.
        var start = DateTime.SpecifyKind(s.Entity.ScheduledAt, DateTimeKind.Utc);
        // En citas virtuales el lugar útil es el enlace de la reunión.
        var where = s.Entity.MeetingLink ?? s.Location;

        // RFC 5545 exige CRLF como fin de línea; el UID estable permite que el calendario actualice en vez de duplicar.
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

    // Formato de fecha y hora UTC de iCalendar (AAAAMMDDTHHMMSSZ).
    private static string Stamp(DateTime utc) => utc.ToString("yyyyMMdd'T'HHmmss'Z'");

    // RFC 5545 §3.3.11: escapar \ ; , y saltos de línea en textos.
    internal static string Escape(string text) => text
        .Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,")
        .Replace("\r\n", "\\n").Replace("\n", "\\n");
}
