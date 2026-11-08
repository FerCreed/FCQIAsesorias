namespace FCQI.Web.Models;

/// <summary>Franja horaria del día para filtrar horarios.</summary>
public enum TimeBand
{
    Any,
    Morning,
    Afternoon,
    Evening
}

public record Option<T>(T Value, string Name)
{
    public override string ToString() => Name;
}

/// <summary>
/// Reglas de los filtros del catálogo (patrón Iterador sobre listas ya
/// cargadas). Son funciones puras: se prueban sin interfaz ni servidor.
/// </summary>
public static class CatalogFilters
{
    public const string All = "Todos";

    public static IReadOnlyList<Option<TimeBand>> TimeBands { get; } =
    [
        new(TimeBand.Any, "Cualquier hora"),
        new(TimeBand.Morning, "Mañana (antes de 12:00)"),
        new(TimeBand.Afternoon, "Tarde (12:00 a 17:00)"),
        new(TimeBand.Evening, "Noche (17:00 en adelante)")
    ];

    public static IReadOnlyList<string> Kinds { get; } = [All, "Profesor", "Estudiante"];

    public static IReadOnlyList<string> Modalities { get; } = [All, "Presencial", "Virtual"];

    public static bool Matches(TimeBand band, int startHour) => band switch
    {
        TimeBand.Morning => startHour < 12,
        TimeBand.Afternoon => startHour is >= 12 and < 17,
        TimeBand.Evening => startHour >= 17,
        _ => true
    };

    /// <summary>Sin días marcados no se filtra; con días, basta coincidir con uno.</summary>
    public static bool Matches(IReadOnlyCollection<int> selectedDays, IEnumerable<int> days)
        => selectedDays.Count == 0 || days.Any(selectedDays.Contains);

    public static bool Matches(string option, string value)
        => option == All || string.Equals(option, value, StringComparison.OrdinalIgnoreCase);

    public static bool Contains(string query, params string[] fields)
        => string.IsNullOrWhiteSpace(query) || fields.Any(f => f.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Fechas del periodo que caen en el día de la semana dado (para el calendario de solicitud).</summary>
    public static IEnumerable<DateTime> DatesOn(int dayOfWeek, DateTime from, DateTime to)
    {
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            if ((int)d.DayOfWeek == dayOfWeek) yield return d;
        }
    }
}
