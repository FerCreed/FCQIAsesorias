namespace FCQI.Application.Sessions.Dtos;

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
    string Topic,
    string Status);

public record CreateSessionRequest(int StudentId, int AdvisorId, int SubjectId, int AvailabilityId, DateTime ScheduledAt, string Topic);

public record UpdateSessionStatusRequest(string Status);
