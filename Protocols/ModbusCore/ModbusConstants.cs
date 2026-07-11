namespace Qenex.QSuite.Protocols.Modbus;

/// <summary>Modbus public function codes (Modbus Application Protocol V1.1b3).</summary>
public static class ModbusFunction
{
    public const byte ReadCoils = 0x01;
    public const byte ReadDiscreteInputs = 0x02;
    public const byte ReadHoldingRegisters = 0x03;
    public const byte ReadInputRegisters = 0x04;
    public const byte WriteSingleCoil = 0x05;
    public const byte WriteSingleRegister = 0x06;
    public const byte WriteMultipleRegisters = 0x10;

    /// <summary>Set on the function code of an exception (negative) response.</summary>
    public const byte ExceptionFlag = 0x80;
}

/// <summary>Modbus exception codes returned in negative responses.</summary>
public static class ModbusExceptionCode
{
    public const byte IllegalFunction = 0x01;
    public const byte IllegalDataAddress = 0x02;
    public const byte IllegalDataValue = 0x03;
    public const byte SlaveDeviceFailure = 0x04;
    public const byte Acknowledge = 0x05;
    public const byte SlaveDeviceBusy = 0x06;
    public const byte MemoryParityError = 0x08;
    public const byte GatewayPathUnavailable = 0x0A;
    public const byte GatewayTargetFailedToRespond = 0x0B;

    public static string Describe(byte code) => code switch
    {
        IllegalFunction => "ILLEGAL FUNCTION (0x01): The function code is not supported by the device.",
        IllegalDataAddress => "ILLEGAL DATA ADDRESS (0x02): The requested address range is not mapped.",
        IllegalDataValue => "ILLEGAL DATA VALUE (0x03): A value in the request is not acceptable.",
        SlaveDeviceFailure => "SLAVE DEVICE FAILURE (0x04): Unrecoverable error while servicing the request.",
        Acknowledge => "ACKNOWLEDGE (0x05): Long-running command accepted, still processing.",
        SlaveDeviceBusy => "SLAVE DEVICE BUSY (0x06): Device busy, retry later.",
        MemoryParityError => "MEMORY PARITY ERROR (0x08): Parity error in extended memory.",
        GatewayPathUnavailable => "GATEWAY PATH UNAVAILABLE (0x0A): Gateway could not allocate a path.",
        GatewayTargetFailedToRespond => "GATEWAY TARGET FAILED TO RESPOND (0x0B): No response from the target device.",
        _ => $"Unknown Modbus exception 0x{code:X2}."
    };
}

/// <summary>The four Modbus data tables.</summary>
public enum ModbusRegisterType
{
    /// <summary>Single-bit, read/write (FC 01 read, FC 05 write).</summary>
    Coil,

    /// <summary>Single-bit, read-only (FC 02).</summary>
    DiscreteInput,

    /// <summary>16-bit, read-only (FC 04).</summary>
    InputRegister,

    /// <summary>16-bit, read/write (FC 03 read, FC 06/16 write).</summary>
    HoldingRegister
}

/// <summary>Word (register) order of multi-register values. Byte order inside one register is
/// always big-endian per the Modbus specification; devices differ only in which register holds
/// the most significant word.</summary>
public enum ModbusWordOrder
{
    /// <summary>Most significant word in the first (lowest-address) register. Default.</summary>
    BigEndian,

    /// <summary>Least significant word in the first register (common on many PLCs).</summary>
    LittleEndian
}
