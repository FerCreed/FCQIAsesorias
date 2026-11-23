using FCQI.Web.Models;

namespace FCQI.Web.Services;

// Lecturas públicas del catálogo: materias, asesores, horarios y sedes.
// Separada de IFcqiApi para poder decorarla con caché sin tocar el resto.
public interface ICatalogApi
{
    Task<List<SubjectItem>> GetSubjectsAsync();
    Task<List<AdvisorItem>> GetAdvisorsAsync(int subjectId);
    Task<List<AvailabilityItem>> GetAvailabilitiesAsync(int advisorId);
    Task<List<LocationItem>> GetLocationsAsync();
    Task<TermItem> GetTermAsync();
}

// Operaciones que requieren sesión. Cada método es un caso de uso de la API.
public interface IFcqiApi
{
    // Autenticación.
    Task<AuthConfigItem> GetAuthConfigAsync();
    Task<AuthResultItem> LoginAsync(string email);
    Task<List<DemoProfileItem>> GetDemoProfilesAsync();

    // Asesorías del usuario. Los archivos (calendario, reportes) llegan como bytes.
    Task<List<SessionItem>> GetSessionsAsync(string scope);
    Task CreateSessionAsync(CreateSessionBody body);
    Task UpdateStatusAsync(int sessionId, string status);
    Task RescheduleAsync(int sessionId, RescheduleBody body);
    Task RecordLogAsync(int sessionId, SessionLogBody body);
    Task<byte[]> GetCalendarAsync(int sessionId);
    Task<byte[]> GetSessionReportAsync(int sessionId);
    Task ContactAdvisorAsync(int advisorId, string message);

    // Horarios del asesor.
    Task<List<AvailabilityItem>> GetMyAvailabilitiesAsync();
    Task CreateAvailabilityAsync(AvailabilityBody body);
    Task UpdateAvailabilityAsync(int id, AvailabilityBody body);
    Task RetireAvailabilityAsync(int id);

    // Dirección y reportes.
    Task<List<AdvisorItem>> GetAdminAdvisorsAsync();
    Task SaveAdvisorSubjectsAsync(int advisorId, List<int> subjectIds);
    Task<byte[]> GetMyReportAsync(string format, DateOnly from, DateOnly to);

    Task<DashboardItem> GetDashboardAsync(ReportFilterItem filter);
    Task<byte[]> GetSessionsExportAsync(string format, ReportFilterItem filter);
    Task<byte[]> GetAdvisorReportAsync(int advisorId, string format, DateOnly from, DateOnly to);
}

// Servicios que dependen de la plataforma (navegador o escritorio). Adaptador:
// los ViewModels no saben si guardan con el diálogo del sistema o del navegador.
public interface IPlatformServices
{
    // Entrega un archivo al usuario. Devuelve false si canceló el diálogo.
    Task<bool> SaveFileAsync(string suggestedName, string mimeType, byte[] content);

    // Abre una URL externa (p. ej. el enlace de la reunión) en el navegador del sistema.
    Task OpenUrlAsync(string url);

    // Preferencias de esta persona en este equipo (p. ej. el zoom). Nulo si no hay.
    string? GetSetting(string key);

    // Guarda una preferencia de esta persona en este equipo.
    void SetSetting(string key, string value);
}
