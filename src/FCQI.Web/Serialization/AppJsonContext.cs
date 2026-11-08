using System.Text.Json.Serialization;
using FCQI.Web.Models;

namespace FCQI.Web.Serialization;

/// <summary>
/// Serialización generada en compilación: en WebAssembly el recorte de código
/// elimina la reflexión que usaría System.Text.Json en tiempo de ejecución.
/// </summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<SubjectItem>))]
[JsonSerializable(typeof(List<AdvisorItem>))]
[JsonSerializable(typeof(List<AvailabilityItem>))]
[JsonSerializable(typeof(List<LocationItem>))]
[JsonSerializable(typeof(List<SessionItem>))]
[JsonSerializable(typeof(SessionItem))]
[JsonSerializable(typeof(DashboardItem))]
[JsonSerializable(typeof(TermItem))]
[JsonSerializable(typeof(AuthResultItem))]
[JsonSerializable(typeof(AuthConfigItem))]
[JsonSerializable(typeof(ErrorMessage))]
[JsonSerializable(typeof(LoginBody))]
[JsonSerializable(typeof(CreateSessionBody))]
[JsonSerializable(typeof(UpdateStatusBody))]
[JsonSerializable(typeof(RescheduleBody))]
[JsonSerializable(typeof(SessionLogBody))]
[JsonSerializable(typeof(ContactBody))]
[JsonSerializable(typeof(AvailabilityBody))]
[JsonSerializable(typeof(List<int>))]
internal partial class AppJsonContext : JsonSerializerContext;
