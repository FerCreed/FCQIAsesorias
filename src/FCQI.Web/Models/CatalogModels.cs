using System.Text.Json.Serialization;

namespace FCQI.Web.Models;

// Modelos del catálogo tal como llegan de la API. Las propiedades [JsonIgnore]
// son textos derivados para la vista: se calculan aquí para que el XAML solo enlace.

// Materia del catálogo con sus carreras y número de asesores.
public class SubjectItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public List<string> Programs { get; set; } = [];
    public int AdvisorCount { get; set; }

    [JsonIgnore] public string AdvisorsText => AdvisorCount switch
    {
        0 => "Sin asesores asignados",
        1 => "1 asesor",
        _ => $"{AdvisorCount} asesores"
    };
}

// Asesor del catálogo con sus materias y resumen de disponibilidad.
public class AdvisorItem
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public bool IsStudent { get; set; }

    // Días con horario publicado (0=domingo…6=sábado).
    public List<int> Days { get; set; } = [];

    public double WeeklyHours { get; set; }
    public string DefaultModality { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Subjects { get; set; } = [];

    [JsonIgnore] public string Summary => $"{Kind}, {Area}";
    [JsonIgnore] public string Availability => Days.Count == 0
        ? "Sin horarios publicados"
        : $"{string.Join(", ", Days.Select(d => DayNames.Of(d)[..3]))} · {WeeklyHours:0.#} h por semana";
}

// Bloque de horario semanal de un asesor.
public class AvailabilityItem
{
    public int Id { get; set; }
    public int AdvisorId { get; set; }
    public int DayOfWeek { get; set; }

    // Horas como texto "HH:mm:ss" (formato de TimeOnly en la API); se recortan al mostrarlas.
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public string Modality { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    [JsonIgnore] public string DayName => DayNames.Of(DayOfWeek);
    [JsonIgnore] public string TimeRange => $"{Short(StartTime)}–{Short(EndTime)}";
    [JsonIgnore] public string Where => $"{Modality}, {Location}";
    [JsonIgnore] public int StartHour => TimeSpan.TryParse(StartTime, out var t) ? t.Hours : 0;

    // Próxima fecha en la que cae el bloque, a partir de mañana. Si coincide
    // con avoid (la cita que se reprograma) salta una semana.
    // shortcut: usa la fecha del dispositivo, no la del campus; si se usa fuera
    // de Baja California cerca de medianoche, mover el cálculo a la API.
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

    // "HH:mm:ss" → "HH:mm"; deja el texto intacto si viene más corto.
    private static string Short(string time) => time.Length >= 5 ? time[..5] : time;
}

// Sede para el formulario de horarios.
public class LocationItem
{
    public short Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Modality { get; set; } = string.Empty;

    // Texto del ComboBox: nombre con modalidad para distinguir sedes virtuales.
    public override string ToString() => $"{Name} ({Modality})";
}

// Ciclo vigente; sus fechas limitan los calendarios de solicitud.
public class TermItem
{
    public short Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
}
