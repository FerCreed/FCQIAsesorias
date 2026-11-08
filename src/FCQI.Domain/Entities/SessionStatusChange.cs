namespace FCQI.Domain.Entities;

/// <summary>
/// Un cambio de estado de una asesoría (tabla historial_estados_sesion). Lo
/// escriben los triggers de la base; la aplicación solo lo lee para reportes.
/// </summary>
public class SessionStatusChange
{
    public long Id { get; set; }
    public long SessionId { get; set; }

    /// <summary>Nulo en la creación de la asesoría.</summary>
    public byte? FromStatusId { get; set; }

    public byte ToStatusId { get; set; }
    public int? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }

    public AdvisorySession Session { get; set; } = null!;
    public SessionStatus? FromStatus { get; set; }
    public SessionStatus ToStatus { get; set; } = null!;
}
