namespace FCQI.Domain.Entities;

public class Subject
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<ProgramSubject> ProgramSubjects { get; set; } = new List<ProgramSubject>();
    public ICollection<AdvisorSubject> AdvisorSubjects { get; set; } = new List<AdvisorSubject>();
}
