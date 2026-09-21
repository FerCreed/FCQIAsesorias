using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using FCQI.Web.Models;
using FCQI.Web.Serialization;

namespace FCQI.Web.Services;

public class FcqiApiClient
{
    public const string BaseUrl = "http://localhost:5016";

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };

    /// <summary>
    /// Token de la sesión. Salvo los endpoints de /api/auth, la API exige
    /// autenticación, así que todas las peticiones lo llevan. Se fija al
    /// iniciar sesión y se borra al salir.
    /// </summary>
    public void UseToken(string? token)
        => _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrWhiteSpace(token) ? null : new AuthenticationHeaderValue("Bearer", token);

    public async Task<AuthConfigItem> GetAuthConfigAsync()
        => await GetAsync($"{BaseUrl}/api/auth/config", AppJsonContext.Default.AuthConfigItem)
           ?? new AuthConfigItem();

    public async Task<List<DemoProfileItem>> GetDemoProfilesAsync()
        => await GetAsync($"{BaseUrl}/api/auth/demo-profiles", AppJsonContext.Default.ListDemoProfileItem)
           ?? [];

    public async Task<AuthResultItem> DemoLoginAsync(string email)
    {
        using var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/auth/demo", new DemoLoginBody { Email = email }, AppJsonContext.Default.DemoLoginBody);

        // En el login un 401 no significa "tu sesión expiró" sino que el correo
        // no está dado de alta, y el motivo viene en el cuerpo: por eso este
        // caso no usa el mensaje genérico.
        await ThrowIfFailedAsync(response, unauthorized: null);

        return await response.Content.ReadFromJsonAsync(AppJsonContext.Default.AuthResultItem)
               ?? throw new InvalidOperationException("Respuesta de login vacía.");
    }

    public async Task<List<SubjectItem>> GetSubjectsAsync()
        => await GetAsync($"{BaseUrl}/api/subjects", AppJsonContext.Default.ListSubjectItem) ?? [];

    public async Task<List<AdvisorItem>> GetAdvisorsAsync(int? subjectId)
    {
        var url = subjectId is null ? $"{BaseUrl}/api/advisors" : $"{BaseUrl}/api/advisors?subjectId={subjectId}";
        return await GetAsync(url, AppJsonContext.Default.ListAdvisorItem) ?? [];
    }

    public async Task<List<AvailabilityItem>> GetAvailabilitiesAsync(int advisorId)
        => await GetAsync($"{BaseUrl}/api/advisors/{advisorId}/availabilities", AppJsonContext.Default.ListAvailabilityItem) ?? [];

    public async Task<List<SessionItem>> GetSessionsAsync(int? studentId, int? advisorId, string? status = null)
    {
        var qs = new List<string>();
        if (studentId is not null) qs.Add($"studentId={studentId}");
        if (advisorId is not null) qs.Add($"advisorId={advisorId}");
        if (!string.IsNullOrWhiteSpace(status)) qs.Add($"status={Uri.EscapeDataString(status)}");
        var url = $"{BaseUrl}/api/sessions" + (qs.Count == 0 ? "" : "?" + string.Join("&", qs));
        return await GetAsync(url, AppJsonContext.Default.ListSessionItem) ?? [];
    }

    public async Task CreateSessionAsync(CreateSessionBody body)
    {
        using var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/sessions", body, AppJsonContext.Default.CreateSessionBody);
        await ThrowIfFailedAsync(response);
    }

    public async Task UpdateStatusAsync(int sessionId, string status)
    {
        using var response = await _http.PatchAsJsonAsync($"{BaseUrl}/api/sessions/{sessionId}/status", new UpdateStatusBody { Status = status }, AppJsonContext.Default.UpdateStatusBody);
        await ThrowIfFailedAsync(response);
    }

    public async Task<List<AdvisorItem>> GetAdminAdvisorsAsync()
        => await GetAsync($"{BaseUrl}/api/admin/advisors", AppJsonContext.Default.ListAdvisorItem) ?? [];

    public async Task SaveAdvisorSubjectsAsync(int advisorId, List<int> subjectIds)
    {
        using var response = await _http.PutAsJsonAsync($"{BaseUrl}/api/admin/advisors/{advisorId}/subjects", subjectIds, AppJsonContext.Default.ListInt32);
        await ThrowIfFailedAsync(response);
    }

    /// <summary>
    /// GET con el mismo tratamiento de errores que el resto.
    ///
    /// Antes las lecturas usaban GetFromJsonAsync, que ante un 403 lanzaba
    /// "Response status code does not indicate success: 403 (Forbidden)". Ese
    /// texto acababa en la barra de estado de la pantalla, en inglés y sin
    /// decir el motivo, que la API sí manda en el cuerpo.
    /// </summary>
    private async Task<T?> GetAsync<T>(string url, JsonTypeInfo<T> typeInfo)
    {
        using var response = await _http.GetAsync(url);
        await ThrowIfFailedAsync(response);
        return await response.Content.ReadFromJsonAsync(typeInfo);
    }

    /// <summary>
    /// Convierte la respuesta de error en un mensaje que se pueda enseñar.
    /// Sin esto, un 403 llegaba a la interfaz como un volcado de excepción.
    /// </summary>
    private static async Task ThrowIfFailedAsync(
        HttpResponseMessage response,
        string? unauthorized = "Tu sesión expiró. Vuelve a entrar.")
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
            message = (await response.Content.ReadFromJsonAsync(AppJsonContext.Default.ErrorMessage))?.Message;
        }
        catch
        {
            // el cuerpo no era JSON; se usa el motivo de la respuesta
        }

        throw new InvalidOperationException(message ?? response.ReasonPhrase ?? "La operación falló.");
    }
}
