using System.ComponentModel;
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
    public ProtocolBase()
    {
        ProtocolVariables = new List<IVariableBase>();
    }
    
    [JsonPropertyName("specification")]
    public ISpecification Specification { get; set; }
    
    //[JsonPropertyName("protocolVariables")]
    [JsonIgnore]
    public IList<IVariableBase> ProtocolVariables { get; set; }

    public void AddVariable(IVariableBase variable)
    {
        ProtocolVariables.Add(variable);
    }

    public void RemoveVariable(IVariableBase variable)
    {
        ProtocolVariables.Remove(variable);
    }

    public void RemoveVariable(string variableName)
    {
        ProtocolVariables.Remove(ProtocolVariables.FirstOrDefault(v => v.Name == variableName) ??
                        throw new InvalidEnumArgumentException("Variable not found"));
    }
}