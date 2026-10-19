namespace FCQI.Domain.Entities;

public class StudentProfile
{
    public int PersonId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;

    /// <summary>Nulo mientras el alumno no declare su carrera.</summary>
    public short? ProgramId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Person Person { get; set; } = null!;
    public AcademicProgram? Program { get; set; }
    public ICollection<AdvisorySession> AdvisorySessions { get; set; } = new List<AdvisorySession>();
}
