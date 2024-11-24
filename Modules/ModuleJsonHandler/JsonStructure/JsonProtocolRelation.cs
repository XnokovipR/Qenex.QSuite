using System.Text.Json.Serialization;

namespace Qenex.QSuite.ModuleJsonHandler.JsonStructure;

public class JsonProtocolRelation
{
    [JsonPropertyName("protocolRefGid")] public Guid ProtocolRefGid { get; set; }
    [JsonPropertyName("variablesRefGids")] public IList<Guid> JsonVariablesRefGids { get; set; }
}