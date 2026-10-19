namespace FCQI.Domain.Entities;

public class ProgramSubject
{
    public short ProgramId { get; set; }
    public int SubjectId { get; set; }
    public byte? Semester { get; set; }

    public AcademicProgram Program { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}
