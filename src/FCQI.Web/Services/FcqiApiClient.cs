using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using FCQI.Web.Models;
using FCQI.Web.Serialization;

namespace FCQI.Web.Services;

/// <summary>
/// Fachada única sobre la API REST. El token lo añade <see cref="AuthTokenHandler"/>
/// y la dirección base llega del anfitrión (navegador o escritorio).
/// </summary>
public class FcqiApiClient : IFcqiApi, ICatalogApi
{
    private static AppJsonContext Json => AppJsonContext.Default;
    private readonly HttpClient _http;

    public FcqiApiClient(HttpClient http) => _http = http;

    public async Task<AuthConfigItem> GetAuthConfigAsync()
        => await GetAsync("api/auth/config", Json.AuthConfigItem) ?? new AuthConfigItem();

    public async Task<AuthResultItem> LoginAsync(string email, string password)
    {
        using var response = await _http.PostAsJsonAsync("api/auth/login", new LoginBody(email, password), Json.LoginBody);
        await ThrowIfFailedAsync(response, unauthorized: null);
        return await response.Content.ReadFromJsonAsync(Json.AuthResultItem)
               ?? throw new InvalidOperationException("Respuesta de inicio de sesión vacía.");
    }

    // Catálogo
    public async Task<List<SubjectItem>> GetSubjectsAsync() => await GetAsync("api/subjects", Json.ListSubjectItem) ?? [];
    public async Task<List<AdvisorItem>> GetAdvisorsAsync(int subjectId) => await GetAsync($"api/advisors?subjectId={subjectId}", Json.ListAdvisorItem) ?? [];
    public async Task<List<AvailabilityItem>> GetAvailabilitiesAsync(int advisorId) => await GetAsync($"api/advisors/{advisorId}/availabilities", Json.ListAvailabilityItem) ?? [];
    public async Task<List<LocationItem>> GetLocationsAsync() => await GetAsync("api/locations", Json.ListLocationItem) ?? [];

    // Asesorías
    public async Task<List<SessionItem>> GetSessionsAsync(string scope) => await GetAsync($"api/sessions?scope={scope}", Json.ListSessionItem) ?? [];
    public Task CreateSessionAsync(CreateSessionBody body) => SendAsync(HttpMethod.Post, "api/sessions", body, Json.CreateSessionBody);
    public Task UpdateStatusAsync(int sessionId, string status) => SendAsync(HttpMethod.Patch, $"api/sessions/{sessionId}/status", new UpdateStatusBody(status), Json.UpdateStatusBody);
    public Task RescheduleAsync(int sessionId, RescheduleBody body) => SendAsync(HttpMethod.Patch, $"api/sessions/{sessionId}/schedule", body, Json.RescheduleBody);
    public Task RecordLogAsync(int sessionId, SessionLogBody body) => SendAsync(HttpMethod.Post, $"api/sessions/{sessionId}/log", body, Json.SessionLogBody);
    public Task<string> GetCalendarAsync(int sessionId) => GetTextAsync($"api/sessions/{sessionId}/calendar.ics");
    public Task ContactAdvisorAsync(int advisorId, string message) => SendAsync(HttpMethod.Post, $"api/advisors/{advisorId}/messages", new ContactBody(message), Json.ContactBody);

    // Horarios del asesor
    public async Task<List<AvailabilityItem>> GetMyAvailabilitiesAsync() => await GetAsync("api/advisors/me/availabilities", Json.ListAvailabilityItem) ?? [];
    public Task CreateAvailabilityAsync(AvailabilityBody body) => SendAsync(HttpMethod.Post, "api/advisors/me/availabilities", body, Json.AvailabilityBody);
    public Task UpdateAvailabilityAsync(int id, AvailabilityBody body) => SendAsync(HttpMethod.Put, $"api/advisors/me/availabilities/{id}", body, Json.AvailabilityBody);
    public Task RetireAvailabilityAsync(int id) => SendAsync<object>(HttpMethod.Delete, $"api/advisors/me/availabilities/{id}", null, null);

    // Dirección
    public async Task<List<AdvisorItem>> GetAdminAdvisorsAsync() => await GetAsync("api/admin/advisors", Json.ListAdvisorItem) ?? [];
    public Task SaveAdvisorSubjectsAsync(int advisorId, List<int> subjectIds) => SendAsync(HttpMethod.Put, $"api/admin/advisors/{advisorId}/subjects", subjectIds, Json.ListInt32);
    public async Task<ReportItem> GetReportAsync() => await GetAsync("api/admin/reports", Json.ReportItem) ?? new ReportItem();
    public Task<string> GetReportCsvAsync() => GetTextAsync("api/admin/reports.csv");

    private async Task<T?> GetAsync<T>(string url, JsonTypeInfo<T> typeInfo)
    {
        using var response = await _http.GetAsync(url);
        await ThrowIfFailedAsync(response);
        return await response.Content.ReadFromJsonAsync(typeInfo);
    }

    private async Task<string> GetTextAsync(string url)
    {
        using var response = await _http.GetAsync(url);
        await ThrowIfFailedAsync(response);
        return await response.Content.ReadAsStringAsync();
    }

    private async Task SendAsync<T>(HttpMethod method, string url, T? body, JsonTypeInfo<T>? typeInfo)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null && typeInfo is not null)
        {
            request.Content = JsonContent.Create(body, typeInfo);
        }

        using var response = await _http.SendAsync(request);
        await ThrowIfFailedAsync(response);
    }

    /// <summary>
    /// Convierte la respuesta de error en un mensaje que se pueda enseñar: la API
    /// manda el motivo en <c>{ message }</c>; sin esto la interfaz mostraba
    /// "Response status code does not indicate success: 403", en inglés y sin motivo.
    /// </summary>
    private static async Task ThrowIfFailedAsync(
        HttpResponseMessage response, string? unauthorized = "Tu sesión expiró. Vuelve a entrar.")
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && unauthorized is not null)
        {
            throw new InvalidOperationException(unauthorized);
        }

        string? message = null;
        try
        {
            message = (await response.Content.ReadFromJsonAsync(Json.ErrorMessage))?.Message;
        }
        catch (System.Text.Json.JsonException)
        {
            // El cuerpo no era JSON (p. ej. una página de error de nginx).
        }

        throw new InvalidOperationException(message ?? response.ReasonPhrase ?? "La operación falló.");
    }
}
