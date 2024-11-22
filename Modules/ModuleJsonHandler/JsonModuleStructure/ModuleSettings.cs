using System.Text.Json.Serialization;
using Qenex.QSuite.Driver;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.ModuleJsonHandler.JsonModuleStructure;

public class ModuleSettings
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("version")]
    public Version? Version { get; set; }
    
    [JsonPropertyName("author")]
    public string? Author { get; set; }
    
    [JsonPropertyName("company")]
    public string? Company { get; set; }
    
    [JsonPropertyName("createdOn")]
    public DateTime CreatedOn { get; set; }

    //public IList<IDriverBase> Drivers { get; set; } = null!;
    
    [JsonPropertyName("variables")]
    public IList<IVariableBase> Variables { get; set; } = null!;
}