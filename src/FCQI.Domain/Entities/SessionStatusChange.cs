namespace FCQI.Domain.Entities;

// Un cambio de estado de una asesoría (tabla historial_estados_sesion). Lo
// escriben los triggers de la base; la aplicación solo lo lee para reportes.
public class SessionStatusChange
{
    // Llave primaria del registro de historial.
    public long Id { get; set; }

    // Asesoría que cambió de estado.
    public long SessionId { get; set; }

    // Nulo en la creación de la asesoría.
    public byte? FromStatusId { get; set; }

    // Estado al que pasó la asesoría.
    public byte ToStatusId { get; set; }

    // Persona que hizo el cambio; nula cuando lo origina un proceso sin usuario.
    public int? ChangedBy { get; set; }

    // Instante del cambio, en UTC.
    public DateTime ChangedAt { get; set; }

    // Motivo opcional (p. ej. razón de cancelación o rechazo).
    public string? Reason { get; set; }

    // Asesoría a la que pertenece el registro.
    public AdvisorySession Session { get; set; } = null!;

    // Estado de origen; nulo en la creación.
    public SessionStatus? FromStatus { get; set; }

    // Estado de destino.
    public SessionStatus ToStatus { get; set; } = null!;
}
