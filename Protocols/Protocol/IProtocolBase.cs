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
    /// Optional narrowing to specific driver Names for protocols designed for one concrete
    /// driver (its data format, not just the transport payload type). Null = any
    /// type-compatible driver; evaluated by the Project Configurator on top of the
    /// transport-type match, never on the communication hot path.
    /// </summary>
    IReadOnlyList<string>? CompatibleDrivers { get; }

    /// <summary>
    /// Logger for protocol-level diagnostics (dropped records, conversion failures).
    /// Propagated by the owning driver; setting it also flows to already-added variables.
    /// </summary>
    ILogger? Logger { get; set; }

    void SetConfiguration();

    /// <summary>
    /// Comm-param template pre-filled when a communicated variable is added to this protocol in
    /// Project Configuration: it lists every supported per-variable parameter with a
    /// representative value so the operator only edits the values. The template lives in the
    /// protocol itself so a plugin dropped into the Protocols folder works without any change
    /// in the host application.
    /// </summary>
    string CreateDefaultCommParam(IVariableBase variable, IEnumerable<IVarEvent> variableEvents);

    /// <summary>
    /// Called by the owning driver when the transport connection is lost (connected = false)
    /// and again when it has been re-established (connected = true). Protocols that keep a live
    /// session (e.g. an XCP CONNECT) use the "lost" signal to abandon the stale session at once
    /// and re-establish it as soon as the transport is back, instead of discovering the loss
    /// slowly through repeated command timeouts. Stateless request/response protocols can ignore
    /// it. Invoked from the driver's run loop, so implementations must be quick and non-blocking.
    /// </summary>
    void OnTransportConnectionChanged(bool connected);

    IProtocolVariable? CreateProtocolVariable(IVariableBase variable, string commParams, bool isCommunicated);
    IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IVarEvent variableEvent, string id);
    IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IEnumerable<IVarEvent> variableEvents, string commParams, bool isCommunicated);
    void AddVariable(IProtocolVariable protocolVariable);
    void RemoveProtocolVariable(IProtocolVariable variable);
    void RemoveProtocolVariable(string variableName);
}
