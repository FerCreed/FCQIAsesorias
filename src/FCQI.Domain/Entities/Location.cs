namespace FCQI.Domain.Entities;

// Sede de la asesoría. Declara su propia modalidad: antes
// availabilities guardaba ambas por separado y el valor
// 'Enlace virtual (Meet/Teams)' implicaba modalidad virtual.
public class Location
{
    // Llave primaria de la sede.
    public short Id { get; set; }

    // Nombre de la sede que se muestra al alumno.
    public string Name { get; set; } = string.Empty;

    // Modalidad implícita de la sede (presencial o virtual).
    public byte ModalityId { get; set; }

    // Indicaciones opcionales para llegar (edificio, salón).
    public string? Details { get; set; }

    // Baja lógica: la sede deja de ofrecerse sin afectar horarios previos.
    public bool IsActive { get; set; } = true;

    // Modalidad de la sede.
    public Modality Modality { get; set; } = null!;
}
