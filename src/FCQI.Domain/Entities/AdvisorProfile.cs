namespace FCQI.Domain.Entities;

// Perfil de asesor de una persona (profesor o alumno par). Concentra los
// horarios, las materias que imparte y las asesorías que atiende.
public class AdvisorProfile
{
    // Llave primaria y foránea a la persona: un perfil de asesor por persona.
    public int PersonId { get; set; }

    // Carrera a la que pertenece el asesor; alimenta el filtro del catálogo.
    public short ProgramId { get; set; }

    // Modalidad que se propone por omisión al crear un horario.
    public byte DefaultModalityId { get; set; }

    // Baja lógica: un asesor inactivo deja de aparecer en el catálogo.
    public bool IsActive { get; set; } = true;

    // Fecha de alta del perfil, en UTC.
    public DateTime CreatedAt { get; set; }

    // Persona dueña del perfil.
    public Person Person { get; set; } = null!;

    // Carrera del asesor.
    public AcademicProgram Program { get; set; } = null!;

    // Modalidad por omisión del asesor.
    public Modality DefaultModality { get; set; } = null!;

    // Bloques de horario semanales que publica el asesor.
    public ICollection<Availability> Availabilities { get; set; } = new List<Availability>();

    // Materias asignadas al asesor por ciclo.
    public ICollection<AdvisorSubject> AdvisorSubjects { get; set; } = new List<AdvisorSubject>();

    // Asesorías que atiende el asesor.
    public ICollection<AdvisorySession> AdvisorySessions { get; set; } = new List<AdvisorySession>();
}
