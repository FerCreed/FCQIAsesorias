namespace FCQI.Application.Subjects.Dtos;

/// <summary>
/// <c>Programs</c>: programas educativos que llevan la materia (filtro del catálogo).
/// <c>AdvisorCount</c>: asesores asignados en el ciclo vigente.
/// </summary>
public record SubjectDto(int Id, string Code, string Name, string Program, IReadOnlyList<string> Programs, int AdvisorCount);
