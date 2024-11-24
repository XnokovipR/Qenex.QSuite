using System.Text.Json.Serialization;

namespace Qenex.QSuite.ModuleJsonHandler.JsonStructure;

public class JsonProtocol
{
    [JsonPropertyName("gid")] public Guid Gid { get; init; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("caption")] public string Caption { get; set; } = string.Empty;
    [JsonPropertyName("version")] public Version Version { get; set; }
}