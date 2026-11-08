using System.Text.Json.Serialization;

namespace FCQI.Web.Models;

public class SubjectItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
}

public class AdvisorItem
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string TutorKind { get; set; } = "Alumno tutor";
    public string DefaultModality { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Subjects { get; set; } = [];

    [JsonIgnore]
    public string Summary => $"{TutorKind}, {Area}";
}

public class AvailabilityItem
{
    public int Id { get; set; }
    public int AdvisorId { get; set; }
    public int DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public string Modality { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    [JsonIgnore] public string DayName => DayNames.Of(DayOfWeek);
    [JsonIgnore] public string TimeRange => $"{Short(StartTime)}–{Short(EndTime)}";
    [JsonIgnore] public string Where => $"{Modality}, {Location}";

    /// <summary>
    /// Próxima fecha en la que cae el bloque, a partir de mañana. Si coincide
    /// con <paramref name="avoid"/> (la cita que se reprograma) salta una semana.
    /// shortcut: usa la fecha del dispositivo, no la del campus; si se usa fuera
    /// de Baja California cerca de medianoche, mover el cálculo a la API.
    /// </summary>
    public DateTime NextOccurrence(DateTime today, DateTime? avoid = null)
    {
        var day = today.Date.AddDays(1);
        while ((int)day.DayOfWeek != DayOfWeek)
        {
            day = day.AddDays(1);
        }

        var start = day + (TimeSpan.TryParse(StartTime, out var t) ? t : TimeSpan.Zero);
        return start == avoid ? start.AddDays(7) : start;
    }

    private static string Short(string time) => time.Length >= 5 ? time[..5] : time;
}

public class LocationItem
{
    public short Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Modality { get; set; } = string.Empty;

    public override string ToString() => $"{Name} ({Modality})";
}
