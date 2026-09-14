namespace FCQI.Domain.Entities;

public class AdvisorSubject
{
    public int AdvisorId { get; set; }
    public int SubjectId { get; set; }

    public Advisor Advisor { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}
