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
    IList<IProtocolVariable> Variables { get; set; }

    IProtocolVariable CreateProtocolVariable(IVariableBase variable, string additionalData);
    void AddVariable(IProtocolVariable protocolVariable);
    void RemoveProtocolVariable(IProtocolVariable variable);
    void RemoveProtocolVariable(string variableName);
}