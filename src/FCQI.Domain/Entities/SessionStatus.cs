namespace FCQI.Domain.Entities;

public class SessionStatus
{
    public byte Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>Si el estado ocupa cupo en el bloque de horario.</summary>
    public bool IsActive { get; set; }
}
