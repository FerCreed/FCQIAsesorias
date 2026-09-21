using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FCQI.Web.Models;

public class SubjectItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;

    [JsonIgnore]
    public string Display => $"{Name} ({Program})";
}

public class AdvisorItem
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string DefaultModality { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Subjects { get; set; } = [];

    [JsonIgnore]
    public string Display => $"{FullName} — {Area} ({DefaultModality})";
}

public class AvailabilityItem
{
    public int Id { get; set; }
    public int AdvisorId { get; set; }
    public int DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Modality { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    [JsonIgnore]
    public string Display
    {
        get
        {
            var day = DayOfWeek switch
            {
                0 => "Domingo",
                1 => "Lunes",
                2 => "Martes",
                3 => "Miércoles",
                4 => "Jueves",
                5 => "Viernes",
                6 => "Sábado",
                _ => "?"
            };
            return $"{day} {StartTime}–{EndTime} · {Modality} · {Location}";
        }
    }
}

public class SessionItem
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int AdvisorId { get; set; }
    public string AdvisorName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int? AvailabilityId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    [JsonIgnore]
    public string Display => $"{Status} · {SubjectName} · {StudentName} → {AdvisorName} · {ScheduledAt:g} · {Topic}";
}

public class DemoProfileItem
{
    public string Role { get; set; } = string.Empty;
    public int ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [JsonIgnore]
    public string Display => $"{Role}: {FullName}";
}

public class AuthResultItem
{
    public string Token { get; set; } = string.Empty;

    /// <summary>Rol predeterminado: el de mayor alcance.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Todos los roles de la persona. Un asesor par trae Asesor y Alumno.
    /// </summary>
    public List<string> Roles { get; set; } = [];

    public int ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AuthConfigItem
{
    public string GoogleClientId { get; set; } = string.Empty;
    public bool GoogleConfigured { get; set; }
}

public class ErrorMessage
{
    public string Message { get; set; } = string.Empty;
}
