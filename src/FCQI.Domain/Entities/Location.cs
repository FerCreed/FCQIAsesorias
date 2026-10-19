namespace FCQI.Domain.Entities;

/// <summary>
/// Sede de la asesoría. Declara su propia modalidad: antes
/// <c>availabilities</c> guardaba ambas por separado y el valor
/// 'Enlace virtual (Meet/Teams)' implicaba modalidad virtual.
/// </summary>
public class Location
{
    public short Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte ModalityId { get; set; }
    public string? Details { get; set; }
    public bool IsActive { get; set; } = true;

    public Modality Modality { get; set; } = null!;
}
