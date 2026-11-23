namespace FCQI.Application.Advisors.Dtos;

// IsStudent, Days (0=domingo…6=sábado) y WeeklyHours alimentan
// los filtros del catálogo: profesor o estudiante, días con horario y horas disponibles.
public record AdvisorDto(
    int Id, string FullName, string Email, string Area, string Kind, string DefaultModality, bool IsActive,
    IReadOnlyList<string> Subjects, bool IsStudent = false, IReadOnlyList<int>? Days = null, double WeeklyHours = 0);

// Bloque de horario tal como lo ve el cliente. Lleva la modalidad y la sede
// ya resueltas a texto para que la interfaz no tenga que cruzar catálogos.
public record AvailabilityDto(int Id, int AdvisorId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int MaxCapacity, string Modality, string Location);

// Bloque semanal que da de alta o edita el asesor (BPMN 7 y 13). Horas locales del campus.
public record SaveAvailabilityRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int MaxCapacity, short LocationId);

// Sede disponible para el formulario de horarios, con su modalidad implícita.
public record LocationDto(short Id, string Name, string Modality);

// Cuerpo de la petición con la que un alumno escribe a un asesor (BPMN 15).
public record ContactAdvisorRequest(string Message);
