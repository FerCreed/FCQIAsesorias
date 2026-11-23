namespace FCQI.Web.Models;

// Respuesta del login. Son clases con setters y no records para que el
// serializador generado las pueble sin constructor.
public class AuthResultItem
{
    // JWT que se envía en cada petición autenticada.
    public string Token { get; set; } = string.Empty;

    // Rol predeterminado: el de mayor alcance.
    public string Role { get; set; } = string.Empty;

    // Todos los roles de la persona. Un asesor par trae Asesor y Alumno.
    public List<string> Roles { get; set; } = [];

    // Identificador de la persona; el mismo para todos sus roles.
    public int ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Configuración pública del login que decide qué opciones de acceso mostrar.
public class AuthConfigItem
{
    public string GoogleClientId { get; set; } = string.Empty;
    public bool GoogleConfigured { get; set; }

    // El servidor acepta entrar solo con el correo (sin Google).
    public bool PasswordlessLogin { get; set; }
}

// Perfil de prueba para entrar sin escribir: una fila por persona y rol.
public class DemoProfileItem
{
    public string Role { get; set; } = string.Empty;
    public int ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    // Texto de la opción en el selector de perfiles.
    public string Display => $"{Role} · {FullName} ({Email})";
}

// Cuerpo { message } con el que la API explica sus errores.
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

// Horas en "HH:mm:ss", el formato de TimeOnly en la API.
public record AvailabilityBody(int DayOfWeek, string StartTime, string EndTime, int MaxCapacity, short LocationId);
