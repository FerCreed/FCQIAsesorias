using FCQI.Web.Models;

namespace FCQI.Web.Services;

/// <summary>Lecturas públicas del catálogo: materias, asesores, horarios y sedes.</summary>
public interface ICatalogApi
{
    Task<List<SubjectItem>> GetSubjectsAsync();
    Task<List<AdvisorItem>> GetAdvisorsAsync(int subjectId);
    Task<List<AvailabilityItem>> GetAvailabilitiesAsync(int advisorId);
    Task<List<LocationItem>> GetLocationsAsync();
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
    Task<string> GetCalendarAsync(int sessionId);
    Task ContactAdvisorAsync(int advisorId, string message);

    Task<List<AvailabilityItem>> GetMyAvailabilitiesAsync();
    Task CreateAvailabilityAsync(AvailabilityBody body);
    Task UpdateAvailabilityAsync(int id, AvailabilityBody body);
    Task RetireAvailabilityAsync(int id);

    Task<List<AdvisorItem>> GetAdminAdvisorsAsync();
    Task SaveAdvisorSubjectsAsync(int advisorId, List<int> subjectIds);
    Task<ReportItem> GetReportAsync();
    Task<string> GetReportCsvAsync();
}

/// <summary>
/// Servicios que dependen de la plataforma (navegador o escritorio). Adaptador:
/// los ViewModels no saben si guardan con el diálogo del sistema o del navegador.
/// </summary>
public interface IPlatformServices
{
    /// <summary>Pide dónde guardar y escribe el texto. Devuelve false si el usuario canceló.</summary>
    Task<bool> SaveTextAsync(string suggestedName, string content);

    Task OpenUrlAsync(string url);
}
