using System.Text.Json.Serialization;

namespace FCQI.Web.Models;

public class SubjectItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;

    [JsonIgnore]
    public string Display => $"{Code} — {Name} ({Program})";
}
