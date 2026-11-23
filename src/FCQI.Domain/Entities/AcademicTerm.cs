namespace FCQI.Domain.Entities;

// Ciclo escolar (semestre). Delimita horarios, asignaciones de materias y
// asesorías, de modo que cada semestre arranca limpio sin borrar el anterior.
public class AcademicTerm
{
    // Llave primaria del ciclo.
    public short Id { get; set; }

    // Clave del ciclo con formato institucional (p. ej. 2026-2).
    public string Code { get; set; } = string.Empty;

    // Nombre legible del ciclo para la interfaz y los reportes.
    public string Name { get; set; } = string.Empty;

    // Primer día del ciclo; DateOnly porque un ciclo no depende de zona horaria.
    public DateOnly StartsOn { get; set; }

    // Último día del ciclo, inclusive.
    public DateOnly EndsOn { get; set; }

    // Marca el ciclo vigente; la aplicación lo usa como ciclo por omisión.
    public bool IsCurrent { get; set; }
}
