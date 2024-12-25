using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.Specification;
using Qenex.QSuite.QVariables;

namespace Qenex.QSuite.Protocol;

/// <summary>
/// Common interface for all protocols.
/// </summary>
public interface IProtocolBase : IComponentSpecification
{
    /// <summary>
    /// Id specifies the protocol instance.
    /// In one driver, there can be only one protocol.
    /// </summary>
    int Id { get; set; }
    
    /// <summary>
    /// Variables that are communicated with the protocol.
    /// </summary>
    IList<IProtocolVariable> Variables { get; set; }

    IProtocolVariable CreateProtocolVariable(IVariableBase variable, string additionalData);
    void AddVariable(IProtocolVariable protocolVariable);
    void RemoveProtocolVariable(IProtocolVariable variable);
    void RemoveProtocolVariable(string variableName);
}