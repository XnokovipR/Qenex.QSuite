using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Specifications.ComponentSpecification;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.Protocol;

/// <summary>
/// Common interface for all protocols.
/// </summary>
public interface IProtocolBase: ICoreCommunication, IComponentSpecification
{
    /// <summary>
    /// Id specifies the protocol instance.
    /// </summary>
    int Id { get; set; }
    
    /// <summary>
    /// Variables that are communicated with the protocol.
    /// </summary>
    IList<IProtocolVariable> Variables { get; set; }
    string RawSettings { get; set; }
    string RawEncryptedSettings { get; set; }

    /// <summary>
    /// Settings template pre-filled when the protocol is added in Project Configuration: it lists
    /// every parameter with a representative value so the operator can see exactly what can be
    /// configured and only edits the values (empty means the protocol has no protocol-level
    /// settings — its parameters are per-variable comm params instead).
    /// </summary>
    string DefaultRawSettings { get; }

    /// <summary>
    /// Logger for protocol-level diagnostics (dropped records, conversion failures).
    /// Propagated by the owning driver; setting it also flows to already-added variables.
    /// </summary>
    ILogger? Logger { get; set; }
    
    void SetConfiguration();

    IProtocolVariable? CreateProtocolVariable(IVariableBase variable, string commParams, bool isCommunicated);
    IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IVarEvent variableEvent, string id);
    IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IEnumerable<IVarEvent> variableEvents, string commParams, bool isCommunicated);
    void AddVariable(IProtocolVariable protocolVariable);
    void RemoveProtocolVariable(IProtocolVariable variable);
    void RemoveProtocolVariable(string variableName);
}
