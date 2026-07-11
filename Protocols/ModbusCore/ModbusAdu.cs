namespace Qenex.QSuite.Protocols.Modbus;

/// <summary>
/// One Modbus application data unit as exchanged with a framer: the unit (slave) identifier, the
/// PDU (function code + data, no framing) and — on Modbus TCP — the MBAP transaction identifier
/// used to correlate a response with its request (always 0 for RTU).
/// </summary>
public sealed record ModbusAdu(byte UnitId, byte[] Pdu, ushort TransactionId = 0)
{
    public byte Function => Pdu.Length > 0 ? Pdu[0] : (byte)0;

    /// <summary>True for a negative (exception) response PDU.</summary>
    public bool IsException => Pdu.Length > 1 && (Pdu[0] & ModbusFunction.ExceptionFlag) != 0;

    /// <summary>Exception code of a negative response (only valid when <see cref="IsException"/>).</summary>
    public byte ExceptionCode => Pdu.Length > 1 ? Pdu[1] : (byte)0;
}
