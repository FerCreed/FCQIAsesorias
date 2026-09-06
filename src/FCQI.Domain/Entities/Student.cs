namespace FCQI.Domain.Entities;

public class Student
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;

    public ICollection<AdvisorySession> AdvisorySessions { get; set; } = new List<AdvisorySession>();
}
