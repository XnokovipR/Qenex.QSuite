using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QSuite.Protocols.Modbus;

/// <summary>
/// The data behind a Modbus slave: the engine translates wire requests into these calls. Reads
/// return false when any address in the range is not mapped (→ ILLEGAL DATA ADDRESS); writes
/// return false for unmapped or read-only targets.
/// </summary>
public interface IModbusDataStore
{
    bool TryReadBits(ModbusRegisterType table, ushort address, int count, out bool[] bits);
    bool TryReadRegisters(ModbusRegisterType table, ushort address, int count, out ushort[] registers);
    ValueTask<bool> TryWriteBitAsync(ushort address, bool value, CancellationToken ct = default);
    ValueTask<bool> TryWriteRegistersAsync(ushort address, ushort[] values, CancellationToken ct = default);
}

/// <summary>
/// Transport-agnostic Modbus slave (server) engine: parses one request ADU, consults the data
/// store and produces the response ADU (or null when the request is not addressed to this unit).
/// Framing is the caller's job — the engine only sees ADUs.
/// </summary>
public sealed class ModbusSlaveEngine(IModbusDataStore store, ILogger? logger = null)
{
    /// <summary>Unit (slave) address this engine answers on.</summary>
    public byte UnitId { get; set; } = 1;

    /// <summary>Accept any unit id (common for Modbus TCP, where the unit id is often 0xFF or
    /// ignored). The response echoes the request's unit id.</summary>
    public bool RespondToAnyUnit { get; set; }

    /// <summary>Processes one request; returns the response ADU, or null when the request is
    /// addressed to another unit and must be ignored silently.</summary>
    public async ValueTask<ModbusAdu?> ProcessRequestAsync(ModbusAdu request, CancellationToken ct = default)
    {
        if (!RespondToAnyUnit && request.UnitId != UnitId)
        {
            return null;
        }

        byte[] responsePdu;
        try
        {
            responsePdu = await BuildResponsePduAsync(request.Pdu, ct);
        }
        catch (ModbusProtocolException e)
        {
            logger?.Log(LogLevel.Warn, $"Modbus slave: malformed request ({e.Message}).");
            responsePdu = ModbusPdu.BuildExceptionResponse(request.Function, ModbusExceptionCode.IllegalDataValue);
        }

        return new ModbusAdu(request.UnitId, responsePdu, request.TransactionId);
    }

    private async ValueTask<byte[]> BuildResponsePduAsync(byte[] requestPdu, CancellationToken ct)
    {
        var request = ModbusPdu.ParseRequest(requestPdu);

        switch (request.Function)
        {
            case ModbusFunction.ReadCoils:
            case ModbusFunction.ReadDiscreteInputs:
            {
                if (request.Count is < 1 or > ModbusPdu.MaxBitsPerRead)
                {
                    return ModbusPdu.BuildExceptionResponse(request.Function, ModbusExceptionCode.IllegalDataValue);
                }

                var table = request.Function == ModbusFunction.ReadCoils
                    ? ModbusRegisterType.Coil
                    : ModbusRegisterType.DiscreteInput;
                return store.TryReadBits(table, request.Address, request.Count, out var bits)
                    ? ModbusPdu.BuildReadBitsResponse(request.Function, bits)
                    : ModbusPdu.BuildExceptionResponse(request.Function, ModbusExceptionCode.IllegalDataAddress);
            }

            case ModbusFunction.ReadHoldingRegisters:
            case ModbusFunction.ReadInputRegisters:
            {
                if (request.Count is < 1 or > ModbusPdu.MaxRegistersPerRead)
                {
                    return ModbusPdu.BuildExceptionResponse(request.Function, ModbusExceptionCode.IllegalDataValue);
                }

                var table = request.Function == ModbusFunction.ReadHoldingRegisters
                    ? ModbusRegisterType.HoldingRegister
                    : ModbusRegisterType.InputRegister;
                return store.TryReadRegisters(table, request.Address, request.Count, out var registers)
                    ? ModbusPdu.BuildReadRegistersResponse(request.Function, registers)
                    : ModbusPdu.BuildExceptionResponse(request.Function, ModbusExceptionCode.IllegalDataAddress);
            }

            case ModbusFunction.WriteSingleCoil:
            {
                // Count carries the raw value field: only 0xFF00 (on) and 0x0000 (off) are legal.
                if (request.Count is not (0xFF00 or 0x0000))
                {
                    return ModbusPdu.BuildExceptionResponse(request.Function, ModbusExceptionCode.IllegalDataValue);
                }

                return await store.TryWriteBitAsync(request.Address, request.Count == 0xFF00, ct)
                    ? ModbusPdu.BuildWriteEchoResponse(requestPdu)
                    : ModbusPdu.BuildExceptionResponse(request.Function, ModbusExceptionCode.IllegalDataAddress);
            }

            case ModbusFunction.WriteSingleRegister:
            {
                return await store.TryWriteRegistersAsync(request.Address, [request.Count], ct)
                    ? ModbusPdu.BuildWriteEchoResponse(requestPdu)
                    : ModbusPdu.BuildExceptionResponse(request.Function, ModbusExceptionCode.IllegalDataAddress);
            }

            case ModbusFunction.WriteMultipleRegisters:
            {
                if (request.Count is < 1 or > ModbusPdu.MaxRegistersPerWrite)
                {
                    return ModbusPdu.BuildExceptionResponse(request.Function, ModbusExceptionCode.IllegalDataValue);
                }

                var values = new ushort[request.Count];
                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = (ushort)((request.Data[i * 2] << 8) | request.Data[i * 2 + 1]);
                }

                return await store.TryWriteRegistersAsync(request.Address, values, ct)
                    ? ModbusPdu.BuildWriteEchoResponse(requestPdu)
                    : ModbusPdu.BuildExceptionResponse(request.Function, ModbusExceptionCode.IllegalDataAddress);
            }

            default:
                return ModbusPdu.BuildExceptionResponse(request.Function, ModbusExceptionCode.IllegalFunction);
        }
    }
}
