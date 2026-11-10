using FCQI.Application.Interfaces;
using FCQI.Domain;
using FCQI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Reports;

/// <summary>Única proyección asesoría → <see cref="SessionRecord"/> de todos los reportes.</summary>
internal static class SessionRecords
{
    public static async Task<List<SessionRecord>> ToRecordsAsync(
        this IQueryable<AdvisorySession> query, IClock clock, CancellationToken cancellationToken)
    {
        var rows = await query.AsNoTracking()
            .OrderBy(s => s.ScheduledAt).ThenBy(s => s.Id)
            .Select(s => new
            {
                s.Id,
                Term = s.Term.Code,
                s.ScheduledAt,
                s.Availability.StartTime,
                s.Availability.EndTime,
                SubjectCode = s.Subject.Code,
                SubjectName = s.Subject.Name,
                s.AdvisorId,
                AdvisorName = s.Advisor.Person.DisplayName,
                AdvisorEmail = s.Advisor.Person.Email,
                AdvisorIsStudent = s.Advisor.Person.StudentProfile != null && s.Advisor.Person.StudentProfile.IsActive,
                s.StudentId,
                StudentName = s.Student.Person.DisplayName,
                s.Student.StudentNumber,
                StudentEmail = s.Student.Person.Email,
                StudentProgram = s.Student.Program != null ? s.Student.Program.Name : null,
                Modality = s.Availability.Location.Modality.Name,
                Location = s.Availability.Location.Name,
                Status = s.Status.Name,
                s.Topic,
                s.MeetingLink,
                Attended = s.Log != null ? s.Log.Attended : (bool?)null,
                Notes = s.Log != null ? s.Log.Notes : null,
                s.CreatedAt,
                s.UpdatedAt,
                Changes = s.History.Count
            })
            .ToListAsync(cancellationToken);

        // Las horas y las fechas se calculan en memoria: MySQL devuelve la resta
        // de TIME como entero y TimeZoneInfo no se traduce a SQL.
        return rows.Select(r =>
        {
            var start = clock.ToLocal(r.ScheduledAt);
            return new SessionRecord(
                r.Id, r.Term, start, start + (r.EndTime - r.StartTime),
                r.SubjectCode, r.SubjectName,
                r.AdvisorId, r.AdvisorName, r.AdvisorEmail,
                r.AdvisorIsStudent ? AdvisorKinds.Student : AdvisorKinds.Professor,
                r.StudentId, r.StudentName, r.StudentNumber, r.StudentEmail, r.StudentProgram ?? "Sin programa",
                r.Modality, r.Location, r.Status, r.Topic, r.MeetingLink,
                r.Attended, r.Notes,
                clock.ToLocal(r.CreatedAt), clock.ToLocal(r.UpdatedAt), r.Changes);
        }).ToList();
    }

    /// <summary>Periodo local del campus → rango UTC semiabierto [desde, hasta+1 día).</summary>
    public static (DateTime FromUtc, DateTime ToUtc) ToUtcRange(this ReportPeriod period, IClock clock)
        => (clock.ToUtc(period.From.ToDateTime(TimeOnly.MinValue)),
            clock.ToUtc(period.To.AddDays(1).ToDateTime(TimeOnly.MinValue)));
}
