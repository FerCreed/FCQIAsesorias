namespace FCQI.Domain.Entities;

public class Advisor
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public ICollection<Availability> Availabilities { get; set; } = new List<Availability>();
    public ICollection<AdvisorySession> AdvisorySessions { get; set; } = new List<AdvisorySession>();
}
