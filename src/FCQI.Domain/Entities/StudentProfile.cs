namespace FCQI.Domain.Entities;

// Perfil de alumno de una persona. Es quien solicita las asesorías.
public class StudentProfile
{
    // Llave primaria y foránea a la persona: un perfil de alumno por persona.
    public int PersonId { get; set; }

    // Matrícula institucional; única por alumno.
    public string StudentNumber { get; set; } = string.Empty;

    // Nulo mientras el alumno no declare su carrera.
    public short? ProgramId { get; set; }

    // Baja lógica: impide solicitar asesorías conservando el historial.
    public bool IsActive { get; set; } = true;

    // Fecha de alta del perfil, en UTC.
    public DateTime CreatedAt { get; set; }

    // Persona dueña del perfil.
    public Person Person { get; set; } = null!;

    // Carrera declarada, si existe.
    public AcademicProgram? Program { get; set; }

    // Asesorías solicitadas por el alumno.
    public ICollection<AdvisorySession> AdvisorySessions { get; set; } = new List<AdvisorySession>();
}
