namespace FCQI.Domain.Entities;

public class AdvisorySession
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int AdvisorId { get; set; }
    public int SubjectId { get; set; }
    public int? AvailabilityId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public Student Student { get; set; } = null!;
    public Advisor Advisor { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public Availability? Availability { get; set; }
}
