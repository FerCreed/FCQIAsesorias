namespace FCQI.Application.Advisors.Dtos;

public record AdvisorDto(int Id, string FullName, string Email, string Area, string Kind, string DefaultModality, bool IsActive, IReadOnlyList<string> Subjects);

public record AvailabilityDto(int Id, int AdvisorId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int MaxCapacity, string Modality, string Location);

/// <summary>Bloque semanal que da de alta o edita el asesor (BPMN 7 y 13). Horas locales del campus.</summary>
public record SaveAvailabilityRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int MaxCapacity, short LocationId);

public record LocationDto(short Id, string Name, string Modality);

public record ContactAdvisorRequest(string Message);
