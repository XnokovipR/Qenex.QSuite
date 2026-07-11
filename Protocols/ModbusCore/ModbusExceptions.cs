namespace Qenex.QSuite.Protocols.Modbus;

/// <summary>The remote device answered with a Modbus exception (negative) response.</summary>
public class ModbusSlaveException(byte function, byte exceptionCode)
    : Exception($"Function 0x{function & ~ModbusFunction.ExceptionFlag:X2} rejected: {ModbusExceptionCode.Describe(exceptionCode)}")
{
    public byte Function { get; } = (byte)(function & ~ModbusFunction.ExceptionFlag);
    public byte ExceptionCode { get; } = exceptionCode;
}

/// <summary>No (matching) response arrived within the configured timeout, after all retries.</summary>
public class ModbusTimeoutException(string operation) : Exception($"Modbus request {operation} timed out.")
{
    public string Operation { get; } = operation;
}

/// <summary>Protocol-level failure: malformed frame/PDU, response mismatch, invalid configuration.</summary>
public class ModbusProtocolException(string message) : Exception(message);
