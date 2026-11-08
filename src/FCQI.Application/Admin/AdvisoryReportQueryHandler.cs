using System.Globalization;
using System.Text;
using FCQI.Application.Common;
using FCQI.Application.Interfaces;
using FCQI.Domain;
using Microsoft.EntityFrameworkCore;

namespace FCQI.Application.Administration;

public record AdvisorReportRow(int AdvisorId, string AdvisorName, int Requested, int Completed, int Attended, double Hours);

public record SubjectDemandRow(int SubjectId, string SubjectName, int Requested);

/// <summary>Métricas del ciclo vigente: demanda, horas impartidas y asistencia.</summary>
public record AdvisoryReportDto(
    string Term,
    int Requested,
    int Completed,
    int Cancelled,
    int Rejected,
    double AttendanceRate,
    double Hours,
    IReadOnlyList<AdvisorReportRow> ByAdvisor,
    IReadOnlyList<SubjectDemandRow> BySubject)
{
    public bool HasData => Requested > 0;
}

/// <summary>
/// Reportes y constancias (BPMN 6). Las horas cuentan solo asesorías
/// realizadas con asistencia: es lo que respalda una constancia de horas.
/// </summary>
public class AdvisoryReportQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly CurrentTerm _term;

    public AdvisoryReportQueryHandler(IApplicationDbContext context, CurrentTerm term)
    {
        _context = context;
        _term = term;
    }

    public async Task<AdvisoryReportDto> HandleAsync(int? advisorId, int? subjectId, CancellationToken cancellationToken = default)
    {
        var term = await _term.GetAsync(cancellationToken);

        var query = _context.AdvisorySessions.AsNoTracking().Where(s => s.TermId == term.Id);
        if (advisorId is not null) query = query.Where(s => s.AdvisorId == advisorId);
        if (subjectId is not null) query = query.Where(s => s.SubjectId == subjectId);

        // Un ciclo tiene a lo sumo unos miles de citas: se agrega en memoria,
        // que es más claro que un GROUP BY con aritmética de TIME en SQL.
        var rows = await query.Select(s => new
            {
                s.AdvisorId,
                AdvisorName = s.Advisor.Person.DisplayName,
                s.SubjectId,
                SubjectName = s.Subject.Name,
                s.StatusId,
                Attended = s.Log != null && s.Log.Attended,
                Duration = s.Availability.EndTime - s.Availability.StartTime
            })
            .ToListAsync(cancellationToken);

        static double Hours(IEnumerable<TimeSpan> spans) => Math.Round(spans.Sum(d => d.TotalHours), 2);

        var completed = rows.Count(r => r.StatusId == SessionStatusIds.Completed);
        var attended = rows.Where(r => r.Attended).ToList();

        return new AdvisoryReportDto(
            term.Code,
            rows.Count,
            completed,
            rows.Count(r => r.StatusId == SessionStatusIds.Cancelled),
            rows.Count(r => r.StatusId == SessionStatusIds.Rejected),
            completed == 0 ? 0 : Math.Round(100.0 * attended.Count / completed, 1),
            Hours(attended.Select(r => r.Duration)),
            rows.GroupBy(r => new { r.AdvisorId, r.AdvisorName })
                .Select(g => new AdvisorReportRow(
                    g.Key.AdvisorId, g.Key.AdvisorName, g.Count(),
                    g.Count(r => r.StatusId == SessionStatusIds.Completed),
                    g.Count(r => r.Attended),
                    Hours(g.Where(r => r.Attended).Select(r => r.Duration))))
                .OrderByDescending(r => r.Hours).ThenBy(r => r.AdvisorName)
                .ToList(),
            rows.GroupBy(r => new { r.SubjectId, r.SubjectName })
                .Select(g => new SubjectDemandRow(g.Key.SubjectId, g.Key.SubjectName, g.Count()))
                .OrderByDescending(r => r.Requested).ThenBy(r => r.SubjectName)
                .ToList());
    }

    /// <summary>Constancia de horas por asesor en CSV (abre en Excel).</summary>
    public static string ToCsv(AdvisoryReportDto report)
    {
        var csv = new StringBuilder($"Ciclo,{report.Term}\r\nAsesor,Solicitadas,Realizadas,Con asistencia,Horas\r\n");
        foreach (var r in report.ByAdvisor)
        {
            csv.Append($"{Quote(r.AdvisorName)},{r.Requested},{r.Completed},{r.Attended},{r.Hours.ToString(CultureInfo.InvariantCulture)}\r\n");
        }

        return csv.ToString();
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
