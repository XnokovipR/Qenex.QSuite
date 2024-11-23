using System.Text.Json.Serialization;
using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.ProtocolVariableSpecification;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.Variable;

/// <summary>
/// The base class for all variables.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class VariableBase<T> : IVariableBase
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("guid")]
    public string Guid { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("caption")]
    public string Caption { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("address")]
    public uint? Address { get; set; }
    
    [JsonPropertyName("length")]
    public uint Length { get; set; }

    /// <summary>
    /// Reference to the modules to which the variable data will be sent.
    /// </summary>
    [JsonIgnore]
    public IList<IComponentSpecification> CommComponents { get; set; }

    /// <summary>
    /// Generic value of the variable.
    /// </summary>
    [JsonIgnore]
    public T Value { get; set; }
}