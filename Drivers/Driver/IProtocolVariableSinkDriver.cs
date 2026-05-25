using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.Drivers.Driver;

public interface IProtocolVariableSinkDriver
{
    bool CanSubscribe(IProtocolVariable sourceVariable);
    Task OnProtocolVariableValueChangedAsync(IProtocolVariable sourceVariable);
}
