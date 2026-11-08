namespace FCQI.Web.Models;

/// <summary>
/// Nombres de rol tal y como los manda la API en <c>roles</c>. Este proyecto
/// no referencia FCQI.Domain —es el cliente— así que se repiten aquí, en un
/// solo sitio.
/// </summary>
public static class UserRoles
{
    public const string Student = "Alumno";
    public const string Advisor = "Asesor";
    public const string Admin = "Directivo";
}

/// <summary>Estados de una asesoría, como los nombra la API.</summary>
public static class SessionStatuses
{
    public const string Pending = "Pendiente";
    public const string Confirmed = "Confirmada";
    public const string Cancelled = "Cancelada";
    public const string Rejected = "Rechazada";
    public const string Completed = "Realizada";
}

public static class DayNames
{
    private static readonly string[] Names = ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

    /// <summary>Lunes a sábado, en el orden en que se muestran en los formularios.</summary>
    public static IReadOnlyList<DayOption> Weekdays { get; } =
        Enumerable.Range(1, 6).Select(d => new DayOption(d, Names[d])).ToList();

    public static string Of(int dayOfWeek) => dayOfWeek is >= 0 and <= 6 ? Names[dayOfWeek] : "?";
}

public record DayOption(int Value, string Name)
{
    public override string ToString() => Name;
}
