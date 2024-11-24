using System.Text.Json.Serialization;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.ModuleJsonHandler.JsonStructure;

public class JsonRoot
{
    [JsonPropertyName("gid")] public Guid Gid { get; init; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("createdOn")] public DateTime CreatedOn { get; set; }
    [JsonPropertyName("version")] public Version Version { get; set; } = null!;
    [JsonPropertyName("author")] public string? Author { get; set; }
    [JsonPropertyName("company")] public string? Company { get; set; }
    [JsonPropertyName("drivers")] public IList<JsonDriver> JsonDrivers { get; set; }
    [JsonPropertyName("protocols")] public IList<JsonProtocol> JsonProtocols { get; set; }
    [JsonPropertyName("variables")] public IList<IVariableBase> JsonVariables { get; set; }
}