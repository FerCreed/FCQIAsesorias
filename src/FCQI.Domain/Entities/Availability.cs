namespace FCQI.Domain.Entities;

// Bloque de horario semanal recurrente. Las horas son de reloj local
// (America/Tijuana), no UTC: describen "los lunes a las 12:00", no un
// instante concreto.
public class Availability
{
    // Llave primaria del bloque.
    public int Id { get; set; }

    // Ciclo en el que el bloque está vigente.
    public short TermId { get; set; }

    // Asesor dueño del bloque.
    public int AdvisorId { get; set; }

    // Día de la semana en que se repite el bloque.
    public DayOfWeek DayOfWeek { get; set; }

    // Hora local de inicio; TimeOnly porque no representa un instante.
    public TimeOnly StartTime { get; set; }

    // Hora local de fin; la base exige que sea posterior al inicio.
    public TimeOnly EndTime { get; set; }

    // Número de alumnos que el bloque admite en una misma fecha.
    public int MaxCapacity { get; set; } = 1;

    // Sede donde se imparte; de ella se deduce la modalidad.
    public short LocationId { get; set; }

    // Baja lógica: retira el bloque sin romper las asesorías que ya lo usan.
    public bool IsActive { get; set; } = true;

    // Fecha de alta del bloque, en UTC.
    public DateTime CreatedAt { get; set; }

    // Ciclo del bloque.
    public AcademicTerm Term { get; set; } = null!;

    // Asesor dueño del bloque.
    public AdvisorProfile Advisor { get; set; } = null!;

    // Sede del bloque.
    public Location Location { get; set; } = null!;

    // Asesorías reservadas en este bloque.
    public ICollection<AdvisorySession> AdvisorySessions { get; set; } = new List<AdvisorySession>();
}
