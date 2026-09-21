namespace FCQI.Domain.Entities;

/// <summary>
/// Materias que un asesor imparte en un ciclo concreto. El ciclo forma parte
/// de la clave: la asignación del semestre pasado ya no se pierde al reasignar.
/// </summary>
public class AdvisorSubject
{
    public short TermId { get; set; }
    public int AdvisorId { get; set; }
    public int SubjectId { get; set; }
    public DateTime CreatedAt { get; set; }

    public AcademicTerm Term { get; set; } = null!;
    public AdvisorProfile Advisor { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}
