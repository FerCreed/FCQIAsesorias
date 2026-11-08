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

public class ReportItem
{
    public string Term { get; set; } = string.Empty;
    public int Requested { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public int Rejected { get; set; }
    public double AttendanceRate { get; set; }
    public double Hours { get; set; }
    public bool HasData { get; set; }
    public List<AdvisorReportItem> ByAdvisor { get; set; } = [];
    public List<SubjectDemandItem> BySubject { get; set; } = [];
}

public class AdvisorReportItem
{
    public int AdvisorId { get; set; }
    public string AdvisorName { get; set; } = string.Empty;
    public int Requested { get; set; }
    public int Completed { get; set; }
    public int Attended { get; set; }
    public double Hours { get; set; }
}

public class SubjectDemandItem
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int Requested { get; set; }
}
