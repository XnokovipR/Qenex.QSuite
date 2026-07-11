using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QSuite.Protocols.Modbus;

/// <summary>
/// Transport-agnostic Modbus master (client) engine. Owns the strictly serialized request/response
/// cycle: one request on the wire at a time, response correlation via the framer (MBAP transaction
/// id on TCP, unit id + deterministic framing on RTU), timeout with simple resend retries (the
/// Modbus-standard recovery — there is no SYNCH equivalent). Transmits through the injected
/// <see cref="Transmitter"/> delegate and consumes raw received bytes via
/// <see cref="OnBytesReceived"/> — it knows nothing about serial ports or sockets.
/// </summary>
public sealed class ModbusMasterEngine(IModbusFramer framer, ILogger? logger = null)
{
    private readonly SemaphoreSlim requestLock = new(1, 1);
    private readonly Lock framerLock = new();
    private volatile TaskCompletionSource<ModbusAdu>? pendingResponse;
    private byte pendingUnitId;
    private ushort pendingTransactionId;
    private ushort nextTransactionId;

    /// <summary>Sends one already-framed ADU to the wire; injected by the hosting protocol.</summary>
    public Func<byte[], CancellationToken, Task>? Transmitter { get; set; }

    /// <summary>Response timeout per request attempt.</summary>
    public int TimeoutMs { get; set; } = 1000;

    /// <summary>How many times a timed-out request is re-sent before giving up.</summary>
    public int MaxRetries { get; set; } = 2;

    #region Receive path

    /// <summary>Feeds raw bytes from the transport. Complete frames are matched against the pending
    /// request; anything else (foreign unit ids, stale transactions) is logged and dropped.</summary>
    public void OnBytesReceived(ReadOnlySpan<byte> data)
    {
        lock (framerLock)
        {
            framer.Append(data);

            while (framer.TryDequeueFrame(out var adu))
            {
                var pending = pendingResponse;
                if (pending == null)
                {
                    logger?.Log(LogLevel.Warn,
                        $"Modbus: unsolicited frame (unit {adu.UnitId}, function 0x{adu.Function:X2}) ignored.");
                    continue;
                }

                if (adu.UnitId != pendingUnitId ||
                    (framer.SupportsTransactionIds && adu.TransactionId != pendingTransactionId))
                {
                    logger?.Log(LogLevel.Warn,
                        $"Modbus: mismatched response (unit {adu.UnitId}, transaction {adu.TransactionId}) ignored.");
                    continue;
                }

                pending.TrySetResult(adu);
            }
        }
    }

    #endregion

    #region Operations

    public async Task<bool[]> ReadBitsAsync(byte unitId, ModbusRegisterType table, ushort address, int count,
        CancellationToken ct = default)
    {
        if (table is not (ModbusRegisterType.Coil or ModbusRegisterType.DiscreteInput))
        {
            throw new ArgumentOutOfRangeException(nameof(table), table, "Bit reads address coils or discrete inputs.");
        }

        if (count is < 1 or > ModbusPdu.MaxBitsPerRead)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Bit reads transfer 1..2000 bits.");
        }

        var function = table == ModbusRegisterType.Coil ? ModbusFunction.ReadCoils : ModbusFunction.ReadDiscreteInputs;
        var request = ModbusPdu.BuildReadRequest(function, address, (ushort)count);
        var response = await ExecuteRequestAsync(unitId, request, $"read {count} bit(s) at {address}", ct);
        return ModbusPdu.ParseReadBitsResponse(response.Pdu, count);
    }

    public async Task<ushort[]> ReadRegistersAsync(byte unitId, ModbusRegisterType table, ushort address, int count,
        CancellationToken ct = default)
    {
        if (table is not (ModbusRegisterType.HoldingRegister or ModbusRegisterType.InputRegister))
        {
            throw new ArgumentOutOfRangeException(nameof(table), table, "Register reads address holding or input registers.");
        }

        if (count is < 1 or > ModbusPdu.MaxRegistersPerRead)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Register reads transfer 1..125 registers.");
        }

        var function = table == ModbusRegisterType.HoldingRegister
            ? ModbusFunction.ReadHoldingRegisters
            : ModbusFunction.ReadInputRegisters;
        var request = ModbusPdu.BuildReadRequest(function, address, (ushort)count);
        var response = await ExecuteRequestAsync(unitId, request, $"read {count} register(s) at {address}", ct);
        return ModbusPdu.ParseReadRegistersResponse(response.Pdu, count);
    }

    public async Task WriteSingleCoilAsync(byte unitId, ushort address, bool value, CancellationToken ct = default)
    {
        var request = ModbusPdu.BuildWriteSingleCoil(address, value);
        var response = await ExecuteRequestAsync(unitId, request, $"write coil at {address}", ct);
        ModbusPdu.ValidateWriteResponse(request, response.Pdu);
    }

    /// <summary>Writes holding registers: FC 06 for a single register, FC 16 for multi-register values.</summary>
    public async Task WriteRegistersAsync(byte unitId, ushort address, ushort[] values, CancellationToken ct = default)
    {
        var request = values.Length == 1
            ? ModbusPdu.BuildWriteSingleRegister(address, values[0])
            : ModbusPdu.BuildWriteMultipleRegisters(address, values);
        var response = await ExecuteRequestAsync(unitId, request, $"write {values.Length} register(s) at {address}", ct);
        ModbusPdu.ValidateWriteResponse(request, response.Pdu);
    }

    #endregion

    #region Request/response engine

    private async Task<ModbusAdu> ExecuteRequestAsync(byte unitId, byte[] requestPdu, string operation, CancellationToken ct)
    {
        var transmitter = Transmitter
                          ?? throw new ModbusProtocolException("Modbus transport is not available (no transmitter injected).");

        await requestLock.WaitAsync(ct);
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                // A fresh transaction id per attempt keeps a late response to a timed-out TCP
                // request from being mistaken for the answer to its retry.
                var transactionId = unchecked(nextTransactionId++);
                var responseSource = new TaskCompletionSource<ModbusAdu>(TaskCreationOptions.RunContinuationsAsynchronously);

                byte[] frame;
                lock (framerLock)
                {
                    pendingUnitId = unitId;
                    pendingTransactionId = transactionId;
                    pendingResponse = responseSource;
                    frame = framer.Encode(new ModbusAdu(unitId, requestPdu, transactionId));
                }

                try
                {
                    await transmitter(frame, ct);

                    var completed = await Task.WhenAny(responseSource.Task, Task.Delay(TimeoutMs, ct));
                    if (completed == responseSource.Task)
                    {
                        var response = await responseSource.Task;
                        if (response.IsException)
                        {
                            throw new ModbusSlaveException(response.Function, response.ExceptionCode);
                        }

                        if (response.Function != requestPdu[0])
                        {
                            throw new ModbusProtocolException(
                                $"Response function 0x{response.Function:X2} does not match request 0x{requestPdu[0]:X2}.");
                        }

                        return response;
                    }

                    ct.ThrowIfCancellationRequested();

                    if (attempt >= MaxRetries)
                    {
                        throw new ModbusTimeoutException(operation);
                    }

                    logger?.Log(LogLevel.Warn,
                        $"Modbus: {operation} timed out; retrying ({attempt + 1}/{MaxRetries}).");
                    lock (framerLock)
                    {
                        framer.Reset(); // drop partial garbage from the aborted exchange (RTU resync)
                    }
                }
                finally
                {
                    pendingResponse = null;
                }
            }
        }
        finally
        {
            requestLock.Release();
        }
    }

    #endregion
}
