namespace FCQI.Web.Models;

// Franja horaria del día para filtrar horarios.
public enum TimeBand
{
    Any,
    Morning,
    Afternoon,
    Evening
}

// Opción genérica valor-etiqueta para los ComboBox de filtros.
public record Option<T>(T Value, string Name)
{
    // El ComboBox muestra ToString(); se devuelve la etiqueta.
    public override string ToString() => Name;
}

// Reglas de los filtros del catálogo (patrón Iterador sobre listas ya
// cargadas). Son funciones puras: se prueban sin interfaz ni servidor.
public static class CatalogFilters
{
    // Valor de "sin filtro" en los ComboBox de texto.
    public const string All = "Todos";

    // Opciones de franja horaria con sus límites explicados en la etiqueta.
    public static IReadOnlyList<Option<TimeBand>> TimeBands { get; } =
    [
        new(TimeBand.Any, "Cualquier hora"),
        new(TimeBand.Morning, "Mañana (antes de 12:00)"),
        new(TimeBand.Afternoon, "Tarde (12:00 a 17:00)"),
        new(TimeBand.Evening, "Noche (17:00 en adelante)")
    ];

    public static IReadOnlyList<string> Kinds { get; } = [All, "Profesor", "Estudiante"];

    public static IReadOnlyList<string> Modalities { get; } = [All, "Presencial", "Virtual"];

    // Si la hora de inicio cae en la franja; Any acepta todo.
    public static bool Matches(TimeBand band, int startHour) => band switch
    {
        TimeBand.Morning => startHour < 12,
        TimeBand.Afternoon => startHour is >= 12 and < 17,
        TimeBand.Evening => startHour >= 17,
        _ => true
    };

    // Sin días marcados no se filtra; con días, basta coincidir con uno.
    public static bool Matches(IReadOnlyCollection<int> selectedDays, IEnumerable<int> days)
        => selectedDays.Count == 0 || days.Any(selectedDays.Contains);

    // Filtro de texto exacto sin distinguir mayúsculas; "Todos" acepta cualquier valor.
    public static bool Matches(string option, string value)
        => option == All || string.Equals(option, value, StringComparison.OrdinalIgnoreCase);

    // Búsqueda libre: la consulta aparece en cualquiera de los campos; vacía acepta todo.
    public static bool Contains(string query, params string[] fields)
        => string.IsNullOrWhiteSpace(query) || fields.Any(f => f.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase));

    // Fechas del periodo que caen en el día de la semana dado (para el calendario de solicitud).
    public static IEnumerable<DateTime> DatesOn(int dayOfWeek, DateTime from, DateTime to)
    {
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            if ((int)d.DayOfWeek == dayOfWeek) yield return d;
        }
    }
}
