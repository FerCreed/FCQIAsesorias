using System.Text.Json.Serialization;

namespace FCQI.Web.Models;

public class SessionItem
{
    private static readonly System.Globalization.CultureInfo Spanish = new("es-MX");

    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int AdvisorId { get; set; }
    public string AdvisorName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int? AvailabilityId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Modality { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? MeetingLink { get; set; }
    public bool HasLog { get; set; }

    [JsonIgnore] public string When => $"{ScheduledAt.ToString("dddd d 'de' MMMM, HH:mm", Spanish)}–{EndsAt:HH:mm}";
    [JsonIgnore] public string People => $"{StudentName} con {AdvisorName}";
    [JsonIgnore] public bool IsPending => Status == SessionStatuses.Pending;
    [JsonIgnore] public bool IsConfirmed => Status == SessionStatuses.Confirmed;
    [JsonIgnore] public bool IsOpen => IsPending || IsConfirmed;
    [JsonIgnore] public bool HasMeetingLink => !string.IsNullOrEmpty(MeetingLink);
}

/// <summary>Tablero de dirección: periodo y métricas de las asesorías.</summary>
public class DashboardItem
{
    public PeriodItem Period { get; set; } = new();
    public MetricsItem Metrics { get; set; } = new();
}

public class PeriodItem
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
}

public class MetricsItem
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public int Rejected { get; set; }
    public int Attended { get; set; }
    public int NoShow { get; set; }
    public double AttendanceRate { get; set; }
    public double CancellationRate { get; set; }
    public double RejectionRate { get; set; }
    public double Hours { get; set; }
    public double AverageLeadDays { get; set; }
    public bool HasData { get; set; }
    public List<AdvisorRowItem> ByAdvisor { get; set; } = [];
    public List<CountRowItem> BySubject { get; set; } = [];
    public List<CountRowItem> ByWeekday { get; set; } = [];
    public List<CountRowItem> ByModality { get; set; } = [];
}

public class AdvisorRowItem
{
    public int AdvisorId { get; set; }
    public string AdvisorName { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public int Requested { get; set; }
    public int Completed { get; set; }
    public int Attended { get; set; }
    public double Hours { get; set; }
}

public class CountRowItem
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Hours { get; set; }
}

/// <summary>Filtros de los reportes de dirección.</summary>
public record ReportFilterItem(DateOnly From, DateOnly To, int? AdvisorId = null, int? SubjectId = null, string? Status = null)
{
    public string Query
    {
        get
        {
            var q = $"from={From:yyyy-MM-dd}&to={To:yyyy-MM-dd}";
            if (AdvisorId is not null) q += $"&advisorId={AdvisorId}";
            if (SubjectId is not null) q += $"&subjectId={SubjectId}";
            if (!string.IsNullOrEmpty(Status)) q += $"&status={Uri.EscapeDataString(Status)}";
            return q;
        }
    }
}
