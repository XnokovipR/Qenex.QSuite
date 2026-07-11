using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.Protocols.Modbus;

public class ModbusProtocolVariable : ProtocolVariable
{
    /// <summary>
    /// True while the protocol itself is applying a value received from the bus (master poll or
    /// remote slave write). The operator-write path is wired to the variable's value-changed
    /// notification, so without this flag every bus update would echo straight back out.
    /// </summary>
    public volatile bool IsUpdatingFromBus;
}
