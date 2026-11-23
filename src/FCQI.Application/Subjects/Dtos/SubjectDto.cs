namespace FCQI.Application.Subjects.Dtos;

// Programs: programas educativos que llevan la materia (filtro del catálogo).
// AdvisorCount: asesores asignados en el ciclo vigente.
public record SubjectDto(int Id, string Code, string Name, string Program, IReadOnlyList<string> Programs, int AdvisorCount);
