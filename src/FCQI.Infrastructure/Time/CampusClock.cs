using FCQI.Application.Interfaces;

namespace FCQI.Infrastructure.Time;

/// <summary>
/// Reloj del campus de la FCQI, en Ensenada (America/Tijuana).
/// </summary>
public class CampusClock : IClock
{
    // Identificador IANA. .NET 8 lo resuelve también en Windows gracias a ICU;
    // el segundo identificador es la variante de Windows, por si el sistema
    // corre sin ICU (modo globalization-invariant).
    private const string Iana = "America/Tijuana";
    private const string Windows = "Pacific Standard Time (Mexico)";

    public TimeZoneInfo Zone { get; }

    public CampusClock()
    {
        Zone = Resolve();
    }

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { Iana, Windows })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // probar el siguiente
            }
            catch (InvalidTimeZoneException)
            {
                // probar el siguiente
            }
        }

        throw new InvalidOperationException(
            $"No se encontró la zona horaria '{Iana}'. En contenedores mínimos hay " +
            "que instalar tzdata, o compilar sin InvariantGlobalization.");
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    public DateTime ToUtc(DateTime local)
    {
        var naive = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);

        // Durante el salto de primavera hay horas locales que no existen
        // (02:00–03:00). Se adelanta al primer instante válido en lugar de
        // reventar, que es lo que haría ConvertTimeToUtc.
        if (Zone.IsInvalidTime(naive))
        {
            naive = naive.AddHours(1);
        }

        // En el salto de otoño una hora local ocurre dos veces. Se toma la
        // primera (horario de verano), que es el criterio de .NET por omisión
        // y el que espera quien agenda.
        return TimeZoneInfo.ConvertTimeToUtc(naive, Zone);
    }

    public DateTime ToLocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);
}
