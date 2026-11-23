namespace FCQI.Domain.Entities;

// Materia del catálogo institucional. Puede pertenecer a varias carreras y
// ser impartida por varios asesores.
public class Subject
{
    // Llave primaria de la materia.
    public int Id { get; set; }

    // Clave oficial de la materia; única en el catálogo.
    public string Code { get; set; } = string.Empty;

    // Nombre de la materia que se muestra al alumno.
    public string Name { get; set; } = string.Empty;

    // Baja lógica: retira la materia del catálogo sin perder historial.
    public bool IsActive { get; set; } = true;

    // Carreras cuyo plan incluye la materia.
    public ICollection<ProgramSubject> ProgramSubjects { get; set; } = new List<ProgramSubject>();

    // Asignaciones de la materia a asesores por ciclo.
    public ICollection<AdvisorSubject> AdvisorSubjects { get; set; } = new List<AdvisorSubject>();
}
