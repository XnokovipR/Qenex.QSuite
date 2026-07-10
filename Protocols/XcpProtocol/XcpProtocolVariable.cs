using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.Protocols.XcpProtocol;

public class XcpProtocolVariable : ProtocolVariable
{
    /// <summary>
    /// True while the protocol itself is applying a value polled from the ECU. The operator-write
    /// path is wired to the variable's value-changed notification, so without this flag every poll
    /// update would echo straight back to the ECU as a DOWNLOAD.
    /// </summary>
    public volatile bool IsUpdatingFromBus;
}
