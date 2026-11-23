namespace FCQI.Application.Sessions.Dtos;

// ScheduledAt y EndsAt viajan en hora local del campus, que es lo que la
// interfaz muestra. ScheduledAtUtc lleva el mismo instante en UTC para los
// clientes que necesiten precisión sin depender de la zona del servidor.
public record SessionDto(
    int Id,
    int StudentId,
    string StudentName,
    int AdvisorId,
    string AdvisorName,
    int SubjectId,
    string SubjectName,
    int? AvailabilityId,
    DateTime ScheduledAt,
    DateTime ScheduledAtUtc,
    string Topic,
    string Status,
    DateTime EndsAt = default,
    string Modality = "",
    string Location = "",
    string? MeetingLink = null,
    bool HasLog = false);

// ScheduledAt se interpreta como hora local del campus.
public record CreateSessionRequest(
    int StudentId, int AdvisorId, int SubjectId, int AvailabilityId, DateTime ScheduledAt, string Topic);

// Estado destino por nombre (p. ej. "Confirmada"), tal como lo muestra la interfaz.
public record UpdateSessionStatusRequest(string Status);

// Nuevo bloque y fecha (hora local del campus) para reprogramar.
public record RescheduleSessionRequest(int AvailabilityId, DateTime ScheduledAt);

// Pase de lista y notas de avance que registra el asesor (BPMN 9).
public record SessionLogRequest(bool Attended, string? Notes);
