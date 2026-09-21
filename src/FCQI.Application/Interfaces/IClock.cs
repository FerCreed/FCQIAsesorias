namespace FCQI.Application.Interfaces;

/// <summary>
/// Conversión entre la hora local del campus y UTC.
///
/// El modelo anterior guardaba hora local en un <c>datetime</c> sin zona. Eso
/// hacía ambiguo el instante de una cita: Ensenada observa horario de verano,
/// así que "12:00" no siempre es el mismo momento absoluto, y las citas que
/// cruzaban el cambio de horario se corrían una hora.
///
/// Ahora la base guarda UTC y la conversión ocurre aquí, una sola vez, con las
/// reglas reales de la zona horaria en lugar de un desplazamiento fijo.
/// </summary>
public interface IClock
{
    TimeZoneInfo Zone { get; }

    DateTime UtcNow { get; }

    /// <summary>Hora de reloj local del campus, sin zona.</summary>
    DateTime LocalNow { get; }

    /// <summary>Hora de reloj local -> instante UTC.</summary>
    DateTime ToUtc(DateTime local);

    /// <summary>Instante UTC -> hora de reloj local.</summary>
    DateTime ToLocal(DateTime utc);
}
