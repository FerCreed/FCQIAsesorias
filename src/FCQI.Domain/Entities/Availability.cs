namespace FCQI.Domain.Entities;

/// <summary>
/// Bloque de horario semanal recurrente. Las horas son de reloj local
/// (America/Tijuana), no UTC: describen "los lunes a las 12:00", no un
/// instante concreto.
/// </summary>
public class Availability
{
    public int Id { get; set; }
    public short TermId { get; set; }
    public int AdvisorId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MaxCapacity { get; set; } = 1;
    public short LocationId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public AcademicTerm Term { get; set; } = null!;
    public AdvisorProfile Advisor { get; set; } = null!;
    public Location Location { get; set; } = null!;
    public ICollection<AdvisorySession> AdvisorySessions { get; set; } = new List<AdvisorySession>();
}
