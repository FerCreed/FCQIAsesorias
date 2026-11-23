namespace FCQI.Domain.Entities;

// Programa educativo (carrera). Antes era el campo de texto libre
// advisors.Area, repetido y expuesto a erratas.
// Se llama AcademicProgram y no Program para no chocar con la clase de
// arranque de la API.
public class AcademicProgram
{
    // Llave primaria; short basta para el número de carreras de la facultad.
    public short Id { get; set; }

    // Clave corta y única de la carrera, usada en catálogos y filtros.
    public string Code { get; set; } = string.Empty;

    // Nombre completo que se muestra en la interfaz.
    public string Name { get; set; } = string.Empty;

    // Baja lógica: una carrera retirada conserva su historial de asesorías.
    public bool IsActive { get; set; } = true;

    // Materias del plan de estudios de la carrera (relación muchos a muchos).
    public ICollection<ProgramSubject> ProgramSubjects { get; set; } = new List<ProgramSubject>();
}
