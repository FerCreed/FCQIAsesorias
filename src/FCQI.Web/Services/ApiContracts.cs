using FCQI.Web.Models;

namespace FCQI.Web.Services;

/// <summary>Lecturas públicas del catálogo: materias, asesores, horarios y sedes.</summary>
public interface ICatalogApi
{
    Task<List<SubjectItem>> GetSubjectsAsync();
    Task<List<AdvisorItem>> GetAdvisorsAsync(int subjectId);
    Task<List<AvailabilityItem>> GetAvailabilitiesAsync(int advisorId);
    Task<List<LocationItem>> GetLocationsAsync();
    Task<TermItem> GetTermAsync();
}

/// <summary>Operaciones que requieren sesión. Cada método es un caso de uso de la API.</summary>
public interface IFcqiApi
{
    Task<AuthConfigItem> GetAuthConfigAsync();
    Task<AuthResultItem> LoginAsync(string email, string password);

    Task<List<SessionItem>> GetSessionsAsync(string scope);
    Task CreateSessionAsync(CreateSessionBody body);
    Task UpdateStatusAsync(int sessionId, string status);
    Task RescheduleAsync(int sessionId, RescheduleBody body);
    Task RecordLogAsync(int sessionId, SessionLogBody body);
    Task<byte[]> GetCalendarAsync(int sessionId);
    Task<byte[]> GetSessionReportAsync(int sessionId);
    Task ContactAdvisorAsync(int advisorId, string message);

    Task<List<AvailabilityItem>> GetMyAvailabilitiesAsync();
    Task CreateAvailabilityAsync(AvailabilityBody body);
    Task UpdateAvailabilityAsync(int id, AvailabilityBody body);
    Task RetireAvailabilityAsync(int id);

    Task<List<AdvisorItem>> GetAdminAdvisorsAsync();
    Task SaveAdvisorSubjectsAsync(int advisorId, List<int> subjectIds);
    Task<byte[]> GetMyReportAsync(string format, DateOnly from, DateOnly to);

    Task<DashboardItem> GetDashboardAsync(ReportFilterItem filter);
    Task<byte[]> GetSessionsExportAsync(string format, ReportFilterItem filter);
    Task<byte[]> GetAdvisorReportAsync(int advisorId, string format, DateOnly from, DateOnly to);
}

/// <summary>
/// Servicios que dependen de la plataforma (navegador o escritorio). Adaptador:
/// los ViewModels no saben si guardan con el diálogo del sistema o del navegador.
/// </summary>
public interface IPlatformServices
{
    /// <summary>Entrega un archivo al usuario. Devuelve false si canceló el diálogo.</summary>
    Task<bool> SaveFileAsync(string suggestedName, string mimeType, byte[] content);

    Task OpenUrlAsync(string url);

    /// <summary>Preferencias de esta persona en este equipo (p. ej. el zoom). Nulo si no hay.</summary>
    string? GetSetting(string key);

    void SetSetting(string key, string value);
}
