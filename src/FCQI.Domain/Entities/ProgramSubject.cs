namespace FCQI.Domain.Entities;

// Relación entre una carrera y una materia de su plan de estudios. Es una
// entidad explícita porque guarda el semestre en que se cursa.
public class ProgramSubject
{
    // Carrera; primera parte de la llave compuesta.
    public short ProgramId { get; set; }

    // Materia; segunda parte de la llave compuesta.
    public int SubjectId { get; set; }

    // Semestre sugerido; nulo para optativas sin semestre fijo.
    public byte? Semester { get; set; }

    // Carrera de la relación.
    public AcademicProgram Program { get; set; } = null!;

    // Materia de la relación.
    public Subject Subject { get; set; } = null!;
}
