namespace FCQI.Application.Advisors.Dtos;

/// <summary>
/// <c>IsStudent</c>, <c>Days</c> (0=domingo…6=sábado) y <c>WeeklyHours</c> alimentan
/// los filtros del catálogo: profesor o estudiante, días con horario y horas disponibles.
/// </summary>
public record AdvisorDto(
    int Id, string FullName, string Email, string Area, string Kind, string DefaultModality, bool IsActive,
    IReadOnlyList<string> Subjects, bool IsStudent = false, IReadOnlyList<int>? Days = null, double WeeklyHours = 0);

public record AvailabilityDto(int Id, int AdvisorId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int MaxCapacity, string Modality, string Location);

/// <summary>Bloque semanal que da de alta o edita el asesor (BPMN 7 y 13). Horas locales del campus.</summary>
public record SaveAvailabilityRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int MaxCapacity, short LocationId);

public record LocationDto(short Id, string Name, string Modality);

public record ContactAdvisorRequest(string Message);
