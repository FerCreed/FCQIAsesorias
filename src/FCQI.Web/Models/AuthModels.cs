namespace FCQI.Web.Models;

public class AuthResultItem
{
    public string Token { get; set; } = string.Empty;

    /// <summary>Rol predeterminado: el de mayor alcance.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Todos los roles de la persona. Un asesor par trae Asesor y Alumno.</summary>
    public List<string> Roles { get; set; } = [];

    public int ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AuthConfigItem
{
    public string GoogleClientId { get; set; } = string.Empty;
    public bool GoogleConfigured { get; set; }

    /// <summary>El servidor acepta entrar solo con el correo (sin Google).</summary>
    public bool PasswordlessLogin { get; set; }
}

/// <summary>Perfil de prueba para entrar sin escribir: una fila por persona y rol.</summary>
public class DemoProfileItem
{
    public string Role { get; set; } = string.Empty;
    public int ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Display => $"{Role} · {FullName} ({Email})";
}

public class ErrorMessage
{
    public string Message { get; set; } = string.Empty;
}

// Cuerpos de las peticiones.

public record LoginBody(string Email);

public record CreateSessionBody(int StudentId, int AdvisorId, int SubjectId, int AvailabilityId, DateTime ScheduledAt, string Topic);

public record UpdateStatusBody(string Status);

public record RescheduleBody(int AvailabilityId, DateTime ScheduledAt);

public record SessionLogBody(bool Attended, string? Notes);

public record ContactBody(string Message);

/// <summary>Horas en "HH:mm:ss", el formato de TimeOnly en la API.</summary>
public record AvailabilityBody(int DayOfWeek, string StartTime, string EndTime, int MaxCapacity, short LocationId);
