namespace FCQI.Web.Models;

// Nombres de rol tal y como los manda la API en roles. Este proyecto
// no referencia FCQI.Domain —es el cliente— así que se repiten aquí, en un
// solo sitio.
public static class UserRoles
{
    public const string Student = "Alumno";
    public const string Advisor = "Asesor";
    public const string Admin = "Directivo";
}

// Estados de una asesoría, como los nombra la API.
public static class SessionStatuses
{
    public const string Pending = "Pendiente";
    public const string Confirmed = "Confirmada";
    public const string Cancelled = "Cancelada";
    public const string Rejected = "Rechazada";
    public const string Completed = "Realizada";
}

// Nombres de los días en español, indexados como DayOfWeek (0=domingo).
public static class DayNames
{
    private static readonly string[] Names = ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

    // Lunes a sábado, en el orden en que se muestran en los formularios.
    public static IReadOnlyList<DayOption> Weekdays { get; } =
        Enumerable.Range(1, 6).Select(d => new DayOption(d, Names[d])).ToList();

    // Nombre del día; "?" ante un valor fuera de rango en vez de lanzar desde la interfaz.
    public static string Of(int dayOfWeek) => dayOfWeek is >= 0 and <= 6 ? Names[dayOfWeek] : "?";
}

// Opción de día para los ComboBox de los formularios.
public record DayOption(int Value, string Name)
{
    // El ComboBox muestra ToString(); se devuelve el nombre en vez del record completo.
    public override string ToString() => Name;
}
