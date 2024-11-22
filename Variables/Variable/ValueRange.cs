using System.Text.Json.Serialization;

namespace Qenex.QSuite.Variable;

public class ValueRange<T>
{
    [JsonPropertyName("minimum")]
    public T Minimum { get; set; }
    
    [JsonPropertyName("maximum")]
    public T Maximum { get; set; }    
}