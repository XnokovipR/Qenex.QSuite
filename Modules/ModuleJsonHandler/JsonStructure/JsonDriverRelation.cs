using System.Text.Json.Serialization;

namespace Qenex.QSuite.ModuleJsonHandler.JsonStructure;

public class JsonDriverRelation
{
    [JsonPropertyName("driverRefGid")] public Guid DriverRefGid { get; set; }
    [JsonPropertyName("protocolsRelations")] public IList<JsonProtocolRelation> JsonProtocolsRelations { get; set; }
}