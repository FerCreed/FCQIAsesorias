using FCQI.Domain;

namespace FCQI.Application.Reports;

public record CountRow(string Name, int Count, double Hours);

public record AdvisorRow(int AdvisorId, string AdvisorName, string Kind, int Requested, int Completed, int Attended, double Hours);

/// <summary>
/// Métricas de BPMN 6 calculadas sobre un conjunto de asesorías: demanda,
/// horas, asistencia, cancelación, anticipación y desgloses. Se calculan en
/// memoria: un ciclo tiene a lo sumo unos miles de citas.
/// </summary>
public record ReportMetrics(
    int Total,
    int Pending,
    int Confirmed,
    int Completed,
    int Cancelled,
    int Rejected,
    int Attended,
    int NoShow,
    double AttendanceRate,
    double CancellationRate,
    double RejectionRate,
    double Hours,
    double AverageLeadDays,
    IReadOnlyList<AdvisorRow> ByAdvisor,
    IReadOnlyList<CountRow> BySubject,
    IReadOnlyList<CountRow> ByWeekday,
    IReadOnlyList<CountRow> ByModality)
{
    public bool HasData => Total > 0;

    public static ReportMetrics From(IReadOnlyCollection<SessionRecord> rows)
    {
        int Count(string status) => rows.Count(r => r.Status == status);
        static double Pct(int part, int whole) => whole == 0 ? 0 : Math.Round(100.0 * part / whole, 1);
        static double Round(double value) => Math.Round(value, 2);

        var completed = Count(SessionStatuses.Completed);
        var attended = rows.Count(r => r.Attended == true);

        IReadOnlyList<CountRow> By(Func<SessionRecord, string> key, IEnumerable<string>? order = null)
        {
            var groups = rows.GroupBy(key)
                .Select(g => new CountRow(g.Key, g.Count(), Round(g.Sum(r => r.HoursWorked))));
            return order is null
                ? groups.OrderByDescending(g => g.Count).ThenBy(g => g.Name).ToList()
                : groups.OrderBy(g => order.ToList().IndexOf(g.Name)).ToList();
        }

        return new ReportMetrics(
            rows.Count,
            Count(SessionStatuses.Pending),
            Count(SessionStatuses.Confirmed),
            completed,
            Count(SessionStatuses.Cancelled),
            Count(SessionStatuses.Rejected),
            attended,
            completed - attended,
            Pct(attended, completed),
            Pct(Count(SessionStatuses.Cancelled), rows.Count),
            Pct(Count(SessionStatuses.Rejected), rows.Count),
            Round(rows.Sum(r => r.HoursWorked)),
            rows.Count == 0 ? 0 : Math.Round(rows.Average(r => (r.ScheduledAt - r.CreatedAt).TotalDays), 1),
            rows.GroupBy(r => new { r.AdvisorId, r.AdvisorName, r.AdvisorKind })
                .Select(g => new AdvisorRow(g.Key.AdvisorId, g.Key.AdvisorName, g.Key.AdvisorKind, g.Count(),
                    g.Count(r => r.Status == SessionStatuses.Completed),
                    g.Count(r => r.Attended == true),
                    Round(g.Sum(r => r.HoursWorked))))
                .OrderByDescending(a => a.Hours).ThenBy(a => a.AdvisorName)
                .ToList(),
            By(r => r.SubjectName),
            By(r => WeekdayName(r.ScheduledAt.DayOfWeek), Weekdays),
            By(r => r.Modality));
    }

    private static readonly string[] Weekdays = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"];

    public static string WeekdayName(DayOfWeek day) => Weekdays[((int)day + 6) % 7];
}
