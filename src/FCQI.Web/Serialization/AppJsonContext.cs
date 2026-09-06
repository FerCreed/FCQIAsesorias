using System.Text.Json.Serialization;
using FCQI.Web.Models;

namespace FCQI.Web.Serialization;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(SubjectItem))]
[JsonSerializable(typeof(List<SubjectItem>))]
internal partial class AppJsonContext : JsonSerializerContext;
