namespace FCQI.Domain.Entities;

/// <summary>
/// Programa educativo (carrera). Antes era el campo de texto libre
/// <c>advisors.Area</c>, repetido y expuesto a erratas.
/// Se llama AcademicProgram y no Program para no chocar con la clase de
/// arranque de la API.
/// </summary>
public class AcademicProgram
{
    public short Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<ProgramSubject> ProgramSubjects { get; set; } = new List<ProgramSubject>();
}
