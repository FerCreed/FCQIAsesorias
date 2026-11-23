namespace FCQI.Domain.Entities;

// Materias que un asesor imparte en un ciclo concreto. El ciclo forma parte
// de la clave: la asignación del semestre pasado ya no se pierde al reasignar.
public class AdvisorSubject
{
    // Ciclo de la asignación; primera parte de la llave compuesta.
    public short TermId { get; set; }

    // Asesor asignado; segunda parte de la llave compuesta.
    public int AdvisorId { get; set; }

    // Materia asignada; tercera parte de la llave compuesta.
    public int SubjectId { get; set; }

    // Fecha de la asignación, en UTC; sirve de auditoría.
    public DateTime CreatedAt { get; set; }

    // Ciclo de la asignación.
    public AcademicTerm Term { get; set; } = null!;

    // Asesor asignado.
    public AdvisorProfile Advisor { get; set; } = null!;

    // Materia asignada.
    public Subject Subject { get; set; } = null!;
}
