using System.Net.Http.Json;
using FCQI.Web.Models;
using FCQI.Web.Serialization;

namespace FCQI.Web.Services;

public class FcqiApiClient
{
    public const string BaseUrl = "http://localhost:5016";

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };

    public async Task<AuthConfigItem> GetAuthConfigAsync()
        => await _http.GetFromJsonAsync($"{BaseUrl}/api/auth/config", AppJsonContext.Default.AuthConfigItem)
           ?? new AuthConfigItem();

    public async Task<List<DemoProfileItem>> GetDemoProfilesAsync()
        => await _http.GetFromJsonAsync($"{BaseUrl}/api/auth/demo-profiles", AppJsonContext.Default.ListDemoProfileItem)
           ?? [];

    public async Task<AuthResultItem> DemoLoginAsync(string email)
    {
        using var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/auth/demo", new DemoLoginBody { Email = email }, AppJsonContext.Default.DemoLoginBody);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(AppJsonContext.Default.AuthResultItem)
               ?? throw new InvalidOperationException("Respuesta de login vacía.");
    }

    public async Task<List<SubjectItem>> GetSubjectsAsync()
        => await _http.GetFromJsonAsync($"{BaseUrl}/api/subjects", AppJsonContext.Default.ListSubjectItem) ?? [];

    public async Task<List<AdvisorItem>> GetAdvisorsAsync(int? subjectId)
    {
        var url = subjectId is null ? $"{BaseUrl}/api/advisors" : $"{BaseUrl}/api/advisors?subjectId={subjectId}";
        return await _http.GetFromJsonAsync(url, AppJsonContext.Default.ListAdvisorItem) ?? [];
    }

    public async Task<List<AvailabilityItem>> GetAvailabilitiesAsync(int advisorId)
        => await _http.GetFromJsonAsync($"{BaseUrl}/api/advisors/{advisorId}/availabilities", AppJsonContext.Default.ListAvailabilityItem) ?? [];

    public async Task<List<SessionItem>> GetSessionsAsync(int? studentId, int? advisorId, string? status = null)
    {
        var qs = new List<string>();
        if (studentId is not null) qs.Add($"studentId={studentId}");
        if (advisorId is not null) qs.Add($"advisorId={advisorId}");
        if (!string.IsNullOrWhiteSpace(status)) qs.Add($"status={Uri.EscapeDataString(status)}");
        var url = $"{BaseUrl}/api/sessions" + (qs.Count == 0 ? "" : "?" + string.Join("&", qs));
        return await _http.GetFromJsonAsync(url, AppJsonContext.Default.ListSessionItem) ?? [];
    }

    public async Task CreateSessionAsync(CreateSessionBody body)
    {
        using var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/sessions", body, AppJsonContext.Default.CreateSessionBody);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.ErrorMessage);
            throw new InvalidOperationException(error?.Message ?? response.ReasonPhrase);
        }
    }

    public async Task UpdateStatusAsync(int sessionId, string status)
    {
        using var response = await _http.PatchAsJsonAsync($"{BaseUrl}/api/sessions/{sessionId}/status", new UpdateStatusBody { Status = status }, AppJsonContext.Default.UpdateStatusBody);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<AdvisorItem>> GetAdminAdvisorsAsync()
        => await _http.GetFromJsonAsync($"{BaseUrl}/api/admin/advisors", AppJsonContext.Default.ListAdvisorItem) ?? [];

    public async Task SaveAdvisorSubjectsAsync(int advisorId, List<int> subjectIds)
    {
        using var response = await _http.PutAsJsonAsync($"{BaseUrl}/api/admin/advisors/{advisorId}/subjects", subjectIds, AppJsonContext.Default.ListInt32);
        response.EnsureSuccessStatusCode();
    }
}
