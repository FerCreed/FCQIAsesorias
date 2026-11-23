namespace FCQI.Application.Interfaces;

// Conversión entre la hora local del campus y UTC.
//
// El modelo anterior guardaba hora local en un datetime sin zona. Eso
// hacía ambiguo el instante de una cita: Ensenada observa horario de verano,
// así que "12:00" no siempre es el mismo momento absoluto, y las citas que
// cruzaban el cambio de horario se corrían una hora.
//
// Ahora la base guarda UTC y la conversión ocurre aquí, una sola vez, con las
// reglas reales de la zona horaria en lugar de un desplazamiento fijo.
public interface IClock
{
    // Zona horaria del campus (America/Tijuana).
    TimeZoneInfo Zone { get; }

    // Instante actual en UTC; abstraído para poder fijarlo en las pruebas.
    DateTime UtcNow { get; }

    // Hora de reloj local del campus, sin zona.
    DateTime LocalNow { get; }

    // Hora de reloj local -> instante UTC.
    DateTime ToUtc(DateTime local);

    // Instante UTC -> hora de reloj local.
    DateTime ToLocal(DateTime utc);
}
