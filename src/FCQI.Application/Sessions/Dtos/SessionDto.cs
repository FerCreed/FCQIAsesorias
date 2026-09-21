namespace FCQI.Application.Sessions.Dtos;

/// <summary>
/// ScheduledAt viaja en hora local del campus, que es lo que la interfaz
/// muestra. ScheduledAtUtc lleva el mismo instante en UTC para los clientes
/// que necesiten precisión sin depender de la zona del servidor.
/// </summary>
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
    string Status);

/// <summary>ScheduledAt se interpreta como hora local del campus.</summary>
public record CreateSessionRequest(
    int StudentId, int AdvisorId, int SubjectId, int AvailabilityId, DateTime ScheduledAt, string Topic);

public record UpdateSessionStatusRequest(string Status);
