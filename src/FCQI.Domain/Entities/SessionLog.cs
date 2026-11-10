namespace FCQI.Domain.Entities;

/// <summary>
/// Bitácora de una asesoría impartida: pase de lista y notas de avance que
/// registra el asesor al terminar (BPMN 9). Una por asesoría.
/// </summary>
public class SessionLog
{
    public long SessionId { get; set; }
    public bool Attended { get; set; }
    public string? Notes { get; set; }
    public int RecordedBy { get; set; }
    public DateTime RecordedAt { get; set; }

    public AdvisorySession Session { get; set; } = null!;
}
