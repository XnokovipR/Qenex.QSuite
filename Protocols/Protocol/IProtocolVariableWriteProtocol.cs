namespace Qenex.QSuite.Protocols.Protocol;

/// <summary>
/// A protocol that executes an operator write of a protocol variable over the driver's transport —
/// anything from a fire-and-forget command line (Pi Zero JSON) to a multi-frame acknowledged
/// transaction (XCP, Modbus). A driver implementing IProtocolVariableCommandDriver delegates the
/// module's command notifications here.
/// </summary>
public interface IProtocolVariableWriteProtocol
{
    /// <summary>True when this protocol owns the variable and its configuration allows writing.</summary>
    bool CanWriteVariable(IProtocolVariable protocolVariable);

    /// <summary>Transfers the variable's current value to the target device.</summary>
    Task WriteVariableAsync(IProtocolVariable protocolVariable, CancellationToken ct = default);
}
