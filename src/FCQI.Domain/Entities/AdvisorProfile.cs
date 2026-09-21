namespace FCQI.Domain.Entities;

public class AdvisorProfile
{
    public int PersonId { get; set; }
    public short ProgramId { get; set; }
    public byte DefaultModalityId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Person Person { get; set; } = null!;
    public AcademicProgram Program { get; set; } = null!;
    public Modality DefaultModality { get; set; } = null!;

    public ICollection<Availability> Availabilities { get; set; } = new List<Availability>();
    public ICollection<AdvisorSubject> AdvisorSubjects { get; set; } = new List<AdvisorSubject>();
    public ICollection<AdvisorySession> AdvisorySessions { get; set; } = new List<AdvisorySession>();
}
