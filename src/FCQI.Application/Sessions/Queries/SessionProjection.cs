using FCQI.Application.Interfaces;
using FCQI.Application.Sessions.Dtos;
using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Sessions.Queries;

// Única proyección de asesoría → SessionDto. La usaban, copiada,
// la consulta de listado y la creación de citas.
internal static class SessionProjection
{
    // Método de extensión: cada consulta aplica sus filtros y orden y luego
    // comparte esta proyección. Se proyecta a un tipo anónimo plano para que
    // EF Core traiga solo las columnas necesarias en una sola consulta.
    public static async Task<List<SessionDto>> ToDtosAsync(
        this IQueryable<AdvisorySession> query, IClock clock, CancellationToken cancellationToken)
    {
        var rows = await query
            .Select(s => new
            {
                s.Id,
                s.StudentId,
                StudentName = s.Student.Person.DisplayName,
                s.AdvisorId,
                AdvisorName = s.Advisor.Person.DisplayName,
                s.SubjectId,
                SubjectName = s.Subject.Name,
                s.AvailabilityId,
                s.ScheduledAt,
                s.Topic,
                Status = s.Status.Name,
                s.Availability.StartTime,
                s.Availability.EndTime,
                Modality = s.Availability.Location.Modality.Name,
                Location = s.Availability.Location.Name,
                s.MeetingLink,
                HasLog = s.Log != null
            })
            .ToListAsync(cancellationToken);

        // La base guarda UTC; la conversión a hora local se hace aquí, fuera
        // de la consulta, porque TimeZoneInfo no se traduce a SQL.
        return rows.Select(r =>
        {
            var start = clock.ToLocal(r.ScheduledAt);
            return new SessionDto(
                (int)r.Id, r.StudentId, r.StudentName,
                r.AdvisorId, r.AdvisorName,
                r.SubjectId, r.SubjectName,
                r.AvailabilityId,
                start,
                DateTime.SpecifyKind(r.ScheduledAt, DateTimeKind.Utc),
                r.Topic, r.Status,
                start + (r.EndTime - r.StartTime),
                r.Modality, r.Location, r.MeetingLink, r.HasLog);
        }).ToList();
    }
}
