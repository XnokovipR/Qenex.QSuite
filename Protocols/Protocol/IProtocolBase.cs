using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.Specification;
using Qenex.QSuite.QVariables;
using Qenex.QSuite.VariableEvents;

namespace Qenex.QSuite.Protocol;

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