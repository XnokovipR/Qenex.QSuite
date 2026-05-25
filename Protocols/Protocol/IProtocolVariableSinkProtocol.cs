namespace Qenex.QSuite.Protocols.Protocol;

public interface IProtocolVariableSinkProtocol
{
    bool CanProcess(IProtocolVariable sourceVariable);
    ValueTask<IProtocolVariable?> ProcessObservedValueAsync(IProtocolVariable sourceVariable, CancellationToken ct = default);
}
