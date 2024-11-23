using System.Text.Json.Serialization;
using Qenex.QSuite.CoreComm;
using Qenex.QSuite.Specification;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.Protocol;

/// <summary>
/// Base class for all protocols.
/// </summary>
public abstract class ProtocolBase : IProtocolBase
{
    [JsonPropertyName("specification")]
    public ISpecification Specification { get; set; }
    
    //[JsonPropertyName("protocolVariables")]
    [JsonIgnore]
    public IList<IVariableBase> ProtocolVariables { get; set; }
}