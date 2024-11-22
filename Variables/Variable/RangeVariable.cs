using System.Text.Json.Serialization;

namespace Qenex.QSuite.Variable;

public class RangeVariable<T> : VariableBase<T>
{
    [JsonPropertyName("rawRange")]
    public ValueRange<T> RawRange { get; set; }
    
    [JsonPropertyName("engineeringRange")]
    public ValueRange<T> EngRange { get; set; }
    
    [JsonPropertyName("unit")]
    public string Unit { get; set; }
}