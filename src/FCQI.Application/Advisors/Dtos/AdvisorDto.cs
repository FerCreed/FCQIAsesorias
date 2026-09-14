namespace FCQI.Application.Advisors.Dtos;

public record AdvisorDto(int Id, string FullName, string Email, string Area, string DefaultModality, bool IsActive, IReadOnlyList<string> Subjects);

public record AvailabilityDto(int Id, int AdvisorId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int MaxCapacity, string Modality, string Location);
