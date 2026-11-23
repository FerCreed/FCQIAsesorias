namespace FCQI.Domain.Entities;

using FCQI.Domain;

// Cita entre un alumno y un asesor.
public class AdvisorySession
{
    // Llave primaria; long porque es la tabla que más crece.
    public long Id { get; set; }

    // Ciclo escolar al que pertenece la asesoría.
    public short TermId { get; set; }

    // Obligatorio: el asesor de la sesión se valida contra el dueño de este
    // bloque mediante una llave foránea compuesta, así que el bloque dejó de
    // poder ser nulo. Para retirar un horario se marca IsActive = false.
    public int AvailabilityId { get; set; }

    // Asesor que atiende; debe coincidir con el dueño del bloque.
    public int AdvisorId { get; set; }

    // Alumno que solicita la asesoría.
    public int StudentId { get; set; }

    // Materia sobre la que trata la asesoría.
    public int SubjectId { get; set; }

    // Instante absoluto en UTC. La conversión desde y hacia la hora local de
    // Ensenada la hace IClock en la capa de aplicación.
    public DateTime ScheduledAt { get; set; }

    // Lugar que ocupa dentro del cupo del bloque; la base impide repetirlo.
    public int SeatNumber { get; set; } = 1;

    // Estado actual; toda asesoría nace pendiente de confirmación.
    public byte StatusId { get; set; } = SessionStatusIds.Pending;

    // Tema o duda que el alumno quiere trabajar.
    public string Topic { get; set; } = string.Empty;

    // Enlace de la reunión virtual; se genera al confirmar (BPMN 10).
    public string? MeetingLink { get; set; }

    // La mantiene un trigger de la base: vale ScheduledAt mientras la sesión
    // ocupa cupo y NULL cuando se cancela. Solo lectura desde la aplicación.
    public DateTime? ActiveAt { get; private set; }

    // Fecha de creación, en UTC.
    public DateTime CreatedAt { get; set; }

    // Fecha de la última modificación, en UTC.
    public DateTime UpdatedAt { get; set; }

    // Ciclo escolar de la asesoría.
    public AcademicTerm Term { get; set; } = null!;

    // Bloque de horario que reserva la asesoría.
    public Availability Availability { get; set; } = null!;

    // Asesor que atiende.
    public AdvisorProfile Advisor { get; set; } = null!;

    // Alumno que solicita.
    public StudentProfile Student { get; set; } = null!;

    // Materia de la asesoría.
    public Subject Subject { get; set; } = null!;

    // Fila del catálogo de estados correspondiente a StatusId.
    public SessionStatus Status { get; set; } = null!;

    // Bitácora del asesor; nula hasta que la asesoría se marca realizada.
    public SessionLog? Log { get; set; }

    // Historial de cambios de estado escrito por los triggers de la base.
    public ICollection<SessionStatusChange> History { get; set; } = new List<SessionStatusChange>();
}
