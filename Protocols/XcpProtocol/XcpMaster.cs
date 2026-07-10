using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QSuite.Protocols.XcpProtocol;

/// <summary>
/// Transport-agnostic XCP master session engine. Owns the strictly serialized request/response
/// cycle (standard communication model: one command, one response), timeout detection with SYNCH
/// recovery and whole-transaction retries, and the connect/disconnect session state. Transmits
/// through the <see cref="Transmitter"/> delegate injected by the hosting protocol and consumes
/// received packets via <see cref="OnPacketReceived"/> — it knows nothing about CAN or TCP framing.
/// </summary>
public sealed class XcpMaster(ILogger? logger = null)
{
    // Classic CAN limits with MAX_CTO=8 and AG=1: RES carries up to 7 data bytes, DOWNLOAD up to 6.
    private const int MaxReadBytesPerPacket = 7;
    private const int MaxWriteBytesPerPacket = 6;

    private readonly SemaphoreSlim requestLock = new(1, 1);
    private volatile TaskCompletionSource<byte[]>? pendingResponse;
    private int pendingEventGeneration;

    /// <summary>Sends one XCP packet to the slave; injected by the hosting protocol while its
    /// transport is usable, null otherwise.</summary>
    public Func<byte[], CancellationToken, Task>? Transmitter { get; set; }

    /// <summary>Response timeout per command; each EV_CMD_PENDING restarts it.</summary>
    public int TimeoutMs { get; set; } = 1000;

    /// <summary>How many times a timed-out transaction is retried (after SYNCH recovery).</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>Codec configured with the slave's byte order after a successful connect.</summary>
    public XcpCodec Codec { get; } = new();

    public bool IsConnected { get; private set; }
    public XcpConnectResponse? ConnectInfo { get; private set; }

    /// <summary>False when the slave has no calibration resource or protects it with seed &amp; key.</summary>
    public bool WritesAllowed { get; private set; }

    #region Receive path

    /// <summary>
    /// Demultiplexes one packet received from the slave by its PID. An incoming packet is never
    /// assumed to answer the pending command — RES/ERR complete it, events and service requests are
    /// out-of-band, DAQ DTOs are ignored until DAQ is implemented.
    /// </summary>
    public void OnPacketReceived(byte[] packet)
    {
        if (packet.Length == 0)
        {
            return;
        }

        switch (XcpPacket.Classify(packet[0]))
        {
            case XcpPacketKind.Response:
            case XcpPacketKind.Error:
                if (pendingResponse?.TrySetResult(packet) != true)
                {
                    logger?.Log(LogLevel.Warn, $"XCP: unsolicited {XcpPacket.Classify(packet[0])} packet (PID 0x{packet[0]:X2}) ignored.");
                }
                break;

            case XcpPacketKind.Event:
                HandleEvent(packet);
                break;

            case XcpPacketKind.ServiceRequest:
                logger?.Log(LogLevel.Info, $"XCP: service request from slave ({BitConverter.ToString(packet)}).");
                break;

            case XcpPacketKind.DaqDto:
                // DAQ is a future extension; DTO packets are ignored for now.
                break;
        }
    }

    private void HandleEvent(byte[] packet)
    {
        var eventCode = packet.Length > 1 ? packet[1] : (byte)0xFF;
        switch (eventCode)
        {
            case XcpEventCode.CmdPending:
                // The slave asks to restart timeout detection; the command must NOT be repeated.
                Interlocked.Increment(ref pendingEventGeneration);
                logger?.Log(LogLevel.Info, "XCP: EV_CMD_PENDING received, restarting timeout.");
                break;

            case XcpEventCode.SessionTerminated:
                IsConnected = false;
                logger?.Log(LogLevel.Warn, "XCP: session terminated by the slave (EV_SESSION_TERMINATED).");
                break;

            default:
                logger?.Log(LogLevel.Info, $"XCP: event 0x{eventCode:X2} from slave.");
                break;
        }
    }

    #endregion

    #region Session

    /// <summary>
    /// CONNECT → adopt byte order and address granularity → GET_STATUS → evaluate seed &amp; key
    /// protection. Throws <see cref="XcpProtocolException"/> for AG&gt;1 slaves (not supported yet).
    /// </summary>
    public Task<XcpStatusResponse> ConnectAsync(CancellationToken ct = default)
    {
        return ExecuteTransactionAsync(async token =>
        {
            var connectPacket = await ExecuteCommandAsync(XcpCodec.BuildConnect(), "CONNECT", token);
            var connect = XcpCodec.ParseConnectResponse(connectPacket);
            Codec.IsBigEndian = connect.IsBigEndian;

            if (connect.AddressGranularity != 1)
            {
                throw new XcpUnsupportedSlaveException(
                    $"Slave uses ADDRESS_GRANULARITY {(connect.AddressGranularity == 0 ? "reserved" : connect.AddressGranularity.ToString())}; only BYTE (AG=1) is supported.");
            }

            ConnectInfo = connect;
            IsConnected = true;

            var statusPacket = await ExecuteCommandAsync(XcpCodec.BuildGetStatus(), "GET_STATUS", token);
            var status = Codec.ParseGetStatusResponse(statusPacket);
            WritesAllowed = connect.SupportsCalibration && !status.IsCalibrationProtected;

            return status;
        }, ct);
    }

    /// <summary>Best-effort DISCONNECT: one attempt, failures are only logged.</summary>
    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        await requestLock.WaitAsync(ct);
        try
        {
            await ExecuteCommandAsync(XcpCodec.BuildDisconnect(), "DISCONNECT", ct);
        }
        catch (Exception e) when (e is XcpErrorException or XcpTimeoutException)
        {
            logger?.Log(LogLevel.Warn, $"XCP: DISCONNECT failed ({e.Message}).");
        }
        finally
        {
            IsConnected = false;
            ConnectInfo = null;
            WritesAllowed = false;
            requestLock.Release();
        }
    }

    #endregion

    #region Memory transfer

    /// <summary>
    /// Reads <paramref name="size"/> bytes (1..8) from the ECU. Up to 7 bytes fit a single
    /// SHORT_UPLOAD; 8-byte values are chained as SHORT_UPLOAD(7) + UPLOAD(1) using the
    /// auto-incremented MTA (each frame individually acknowledged, no block mode). The chained
    /// transfer is not atomic.
    /// </summary>
    public Task<byte[]> ReadMemoryAsync(byte addressExtension, uint address, int size, CancellationToken ct = default)
    {
        EnsureConnected();
        if (size is < 1 or > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "XCP reads transfer 1..8 bytes.");
        }

        return ExecuteTransactionAsync(async token =>
        {
            var result = new byte[size];

            var firstCount = Math.Min(size, MaxReadBytesPerPacket);
            var first = await ExecuteCommandAsync(
                Codec.BuildShortUpload((byte)firstCount, addressExtension, address), "SHORT_UPLOAD", token);
            CopyResponseData(first, "SHORT_UPLOAD", result.AsSpan(0, firstCount));

            if (size > firstCount)
            {
                // SHORT_UPLOAD leaves the MTA behind the uploaded block (spec 1.6.1.2.8), so the
                // remainder continues from there.
                var remaining = size - firstCount;
                var second = await ExecuteCommandAsync(XcpCodec.BuildUpload((byte)remaining), "UPLOAD", token);
                CopyResponseData(second, "UPLOAD", result.AsSpan(firstCount, remaining));
            }

            return result;
        }, ct);
    }

    /// <summary>
    /// Writes bytes to the ECU as SET_MTA + DOWNLOAD (never SHORT_DOWNLOAD). Values larger than
    /// 6 bytes are chained as consecutive DOWNLOADs at the auto-incremented MTA (8 bytes = 6+2),
    /// each individually acknowledged. On timeout the whole transaction retries, which re-issues
    /// SET_MTA — the MTA is never trusted across a recovery.
    /// </summary>
    public Task WriteMemoryAsync(byte addressExtension, uint address, byte[] data, CancellationToken ct = default)
    {
        EnsureConnected();
        if (!WritesAllowed)
        {
            throw new XcpProtocolException(
                "Calibration writes are not available: the slave's calibration resource is missing or requires seed & key.");
        }

        if (data.Length is < 1 or > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(data), data.Length, "XCP writes transfer 1..8 bytes.");
        }

        return ExecuteTransactionAsync<object?>(async token =>
        {
            await ExecuteCommandAsync(Codec.BuildSetMta(addressExtension, address), "SET_MTA", token);

            for (var offset = 0; offset < data.Length; offset += MaxWriteBytesPerPacket)
            {
                var chunk = data.AsSpan(offset, Math.Min(MaxWriteBytesPerPacket, data.Length - offset));
                await ExecuteCommandAsync(XcpCodec.BuildDownload(chunk), "DOWNLOAD", token);
            }

            return null;
        }, ct);
    }

    private void EnsureConnected()
    {
        if (!IsConnected)
        {
            throw new XcpProtocolException("Not connected to the XCP slave.");
        }
    }

    private static void CopyResponseData(byte[] response, string command, Span<byte> destination)
    {
        // RES layout for AG=1: 0xFF followed by the data elements; padding may follow.
        if (response.Length < 1 + destination.Length)
        {
            throw new XcpProtocolException(
                $"{command} response carries {response.Length - 1} data bytes, expected {destination.Length}.");
        }

        response.AsSpan(1, destination.Length).CopyTo(destination);
    }

    #endregion

    #region Request/response engine

    /// <summary>
    /// Runs one transaction (a sequence of commands that must not be interleaved with others) under
    /// the request lock. On timeout the command processor is re-synchronized with SYNCH and the
    /// whole transaction retried up to <see cref="MaxRetries"/> times — MTA-dependent sequences
    /// thereby naturally re-issue their SET_MTA.
    /// </summary>
    private async Task<TResult> ExecuteTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> transaction, CancellationToken ct)
    {
        await requestLock.WaitAsync(ct);
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    return await transaction(ct);
                }
                catch (XcpTimeoutException e) when (attempt < MaxRetries)
                {
                    logger?.Log(LogLevel.Warn, $"XCP: {e.Message} Re-synchronizing (attempt {attempt + 1}/{MaxRetries}).");
                    await TrySynchAsync(ct);
                }
            }
        }
        finally
        {
            requestLock.Release();
        }
    }

    /// <summary>Sends SYNCH after a timeout. The expected answer is ERR_CMD_SYNCH; its absence is
    /// only logged — the following retry will fail fast if the slave is really gone.</summary>
    private async Task TrySynchAsync(CancellationToken ct)
    {
        try
        {
            await ExecuteCommandAsync(XcpCodec.BuildSynch(), "SYNCH", ct);
            logger?.Log(LogLevel.Warn, "XCP: SYNCH got a positive response; expected ERR_CMD_SYNCH.");
        }
        catch (XcpErrorException e) when (e.ErrorCode == XcpErrorCode.CmdSynch)
        {
            // ERR_CMD_SYNCH is the defined (successful) answer to SYNCH.
        }
        catch (XcpTimeoutException)
        {
            logger?.Log(LogLevel.Warn, "XCP: SYNCH itself timed out.");
        }
    }

    /// <summary>
    /// One command, one response: transmits the packet and waits for RES/ERR. EV_CMD_PENDING
    /// restarts the timeout window without re-sending. ERR becomes <see cref="XcpErrorException"/>;
    /// a genuine timeout becomes <see cref="XcpTimeoutException"/> (recovery is the transaction
    /// wrapper's job). Must only be called while holding the request lock.
    /// </summary>
    private async Task<byte[]> ExecuteCommandAsync(byte[] command, string commandName, CancellationToken ct)
    {
        var transmitter = Transmitter
                          ?? throw new XcpProtocolException("XCP transport is not available (no transmitter injected).");

        var responseSource = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        pendingResponse = responseSource;
        try
        {
            await transmitter(command, ct);

            while (true)
            {
                var generationAtWaitStart = Volatile.Read(ref pendingEventGeneration);
                var completed = await Task.WhenAny(responseSource.Task, Task.Delay(TimeoutMs, ct));

                if (completed == responseSource.Task)
                {
                    var packet = await responseSource.Task;
                    if (packet[0] == 0xFE)
                    {
                        throw new XcpErrorException(packet.Length > 1 ? packet[1] : XcpErrorCode.Generic);
                    }

                    return packet;
                }

                ct.ThrowIfCancellationRequested();

                if (Volatile.Read(ref pendingEventGeneration) != generationAtWaitStart)
                {
                    continue; // EV_CMD_PENDING arrived — restart the timeout, do not repeat the command.
                }

                throw new XcpTimeoutException(commandName);
            }
        }
        finally
        {
            pendingResponse = null;
        }
    }

    #endregion
}
