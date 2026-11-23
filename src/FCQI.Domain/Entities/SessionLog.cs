namespace FCQI.Domain.Entities;

// Bitácora de una asesoría impartida: pase de lista y notas de avance que
// registra el asesor al terminar (BPMN 9). Una por asesoría.
public class SessionLog
{
    // Llave primaria y foránea a la asesoría: garantiza una bitácora por asesoría.
    public long SessionId { get; set; }

    // Pase de lista: si el alumno se presentó.
    public bool Attended { get; set; }

    // Notas de avance opcionales del asesor.
    public string? Notes { get; set; }

    // Persona que registró la bitácora (asesor o dirección).
    public int RecordedBy { get; set; }

    // Fecha del registro, en UTC.
    public DateTime RecordedAt { get; set; }

    // Asesoría a la que pertenece la bitácora.
    public AdvisorySession Session { get; set; } = null!;
}
