using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Specifications.ComponentSpecification;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.Protocol;

/// <summary>
/// Common interface for all protocols.
/// </summary>
public interface IProtocolBase: IComponentSpecification
{
    /// <summary>
    /// Id specifies the protocol instance.
    /// </summary>
    int Id { get; set; }
    
    /// <summary>
    /// Variables that are communicated with the protocol.
    /// </summary>
    IList<IProtocolVariable> Variables { get; set; }
    
    void SetConfiguration(string rawSettings, string rawEncryptedSettings);

    IProtocolVariable? CreateProtocolVariable(IVariableBase variable, string commParams, bool isCommunicated);
    IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IEnumerable<IVarEvent> variableEvents, string commParams, bool isCommunicated);
    void AddVariable(IProtocolVariable protocolVariable);
    void RemoveProtocolVariable(IProtocolVariable variable);
    void RemoveProtocolVariable(string variableName);
    
    IEnumerable<T> Encode<T>(IEnumerable<IProtocolVariable> protocolVariables);
    IEnumerable<IProtocolVariable> Decode<T>(IEnumerable<T> data);
    
}