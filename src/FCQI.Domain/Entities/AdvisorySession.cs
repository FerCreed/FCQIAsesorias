namespace FCQI.Domain.Entities;

using FCQI.Domain;

/// <summary>
/// Cita entre un alumno y un asesor.
/// </summary>
public class AdvisorySession
{
    public long Id { get; set; }
    public short TermId { get; set; }

    /// <summary>
    /// Obligatorio: el asesor de la sesión se valida contra el dueño de este
    /// bloque mediante una llave foránea compuesta, así que el bloque dejó de
    /// poder ser nulo. Para retirar un horario se marca IsActive = false.
    /// </summary>
    public int AvailabilityId { get; set; }

    public int AdvisorId { get; set; }
    public int StudentId { get; set; }
    public int SubjectId { get; set; }

    /// <summary>
    /// Instante absoluto en UTC. La conversión desde y hacia la hora local de
    /// Ensenada la hace <c>IClock</c> en la capa de aplicación.
    /// </summary>
    public DateTime ScheduledAt { get; set; }

    public int SeatNumber { get; set; } = 1;
    public byte StatusId { get; set; } = SessionStatusIds.Pending;
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// La mantiene un trigger de la base: vale ScheduledAt mientras la sesión
    /// ocupa cupo y NULL cuando se cancela. Solo lectura desde la aplicación.
    /// </summary>
    public DateTime? ActiveAt { get; private set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public AcademicTerm Term { get; set; } = null!;
    public Availability Availability { get; set; } = null!;
    public AdvisorProfile Advisor { get; set; } = null!;
    public StudentProfile Student { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public SessionStatus Status { get; set; } = null!;
}
