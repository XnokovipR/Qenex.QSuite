using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.Drivers.Driver;

public interface IProtocolVariableCommandDriver
{
    bool CanSendCommand(IProtocolVariable protocolVariable);
    Task OnProtocolVariableCommandAsync(IProtocolVariable protocolVariable, CancellationToken ct = default);
}
