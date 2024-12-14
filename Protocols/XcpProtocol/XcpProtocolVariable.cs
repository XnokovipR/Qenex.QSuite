using Qenex.QSuite.Protocol;

namespace Qenex.QSuite.Protocols.XcpProtocol;

public class XcpProtocolVariable : ProtocolVariable
{
    public string Address { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
}