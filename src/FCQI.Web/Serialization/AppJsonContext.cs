using System.Text.Json.Serialization;
using FCQI.Web.Models;

namespace FCQI.Web.Serialization;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SubjectItem))]
[JsonSerializable(typeof(List<SubjectItem>))]
[JsonSerializable(typeof(AdvisorItem))]
[JsonSerializable(typeof(List<AdvisorItem>))]
[JsonSerializable(typeof(AvailabilityItem))]
[JsonSerializable(typeof(List<AvailabilityItem>))]
[JsonSerializable(typeof(SessionItem))]
[JsonSerializable(typeof(List<SessionItem>))]
[JsonSerializable(typeof(DemoProfileItem))]
[JsonSerializable(typeof(List<DemoProfileItem>))]
[JsonSerializable(typeof(AuthResultItem))]
[JsonSerializable(typeof(AuthConfigItem))]
[JsonSerializable(typeof(ErrorMessage))]
[JsonSerializable(typeof(CreateSessionBody))]
[JsonSerializable(typeof(UpdateStatusBody))]
[JsonSerializable(typeof(DemoLoginBody))]
[JsonSerializable(typeof(GoogleLoginBody))]
[JsonSerializable(typeof(List<int>))]
internal partial class AppJsonContext : JsonSerializerContext;

public class CreateSessionBody
{
    public int StudentId { get; set; }
    public int AdvisorId { get; set; }
    public int SubjectId { get; set; }
    public int AvailabilityId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Topic { get; set; } = string.Empty;
}

public class UpdateStatusBody
{
    public string Status { get; set; } = string.Empty;
}

public class DemoLoginBody
{
    public string Email { get; set; } = string.Empty;
}

public class GoogleLoginBody
{
    public string IdToken { get; set; } = string.Empty;
}
