namespace Qenex.QSuite.Protocols.Protocol;

public interface IProtocolVariableCommandProtocol
{
    bool CanEncodeCommand(IProtocolVariable protocolVariable);
    string? EncodeCommand(IProtocolVariable protocolVariable);
}
