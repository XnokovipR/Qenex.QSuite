using Qenex.QSuite.CoreComm;
using Qenex.QSuite.Specification;
using Qenex.QSuite.QVariables;

namespace Qenex.QSuite.Protocol;

/// <summary>
/// Common interface for all protocols.
/// </summary>
public interface IProtocolBase
{
    /// <summary>
    /// Id specifies the protocol instance.
    /// In one driver, there can be only one protocol.
    /// </summary>
    int Id { get; set; }
    
    /// <summary>
    /// Protocol specification.
    /// </summary>
    ISpecification Specification { get;  }
    
    /// <summary>
    /// Variables that are communicated with the protocol.
    /// </summary>
    IList<IProtocolVariable> ProtocolVariables { get; set; }
    
    public void AddVariable(IProtocolVariable variable);
    public void AddVariables(IEnumerable<IProtocolVariable> variables);
    public void RemoveVariable(IProtocolVariable variable);
    public void RemoveVariable(string variableName);
}