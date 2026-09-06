namespace FCQI.Domain.Entities;

public class Availability
{
    public int Id { get; set; }
    public int AdvisorId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MaxCapacity { get; set; }
    public string Modality { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public Advisor Advisor { get; set; } = null!;
    public ICollection<AdvisorySession> AdvisorySessions { get; set; } = new List<AdvisorySession>();
}
