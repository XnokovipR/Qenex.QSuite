using System.Reflection;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.XcpProtocol;

/// <summary>
/// Simplified XCP master (ASAM MCD-1 XCP 1.1) over a CAN driver. Reads ECU memory by polling
/// SHORT_UPLOAD per variable (address/size from the module XML, interval from the variable's
/// periodic event) and writes on operator request via SET_MTA + DOWNLOAD. Standard communication
/// model only (one command, one response); no DAQ, no block mode, no seed &amp; key. The packet codec
/// (XcpCodec) and session engine (XcpMaster) are transport-agnostic; this class binds them to CAN
/// framing: commands go out on MasterID padded to DLC 8, responses come back on SlaveID.
/// </summary>
public class Xcp : ProtocolBase<CanFrame>, ITransportProtocol<CanFrame>, IProtocolVariableWriteProtocol
{
    private const int ReconnectDelayMs = 2000;
    private const int MaxConsecutiveBusFailures = 5;
    private const int SchedulerIdleMs = 50;

    private XcpSessionSettings settings = new();
    private string? configurationError;

    private volatile XcpMaster? master;
    private volatile Func<CanFrame, CancellationToken, Task>? transmitter;

    private volatile bool exitRequested;
    private CancellationTokenSource? runCts;
    private Task? runTask;

    public Xcp()
    {
        Specification = new SpecificationBase
        {
            Name = "XcpProtocol",
            Label = "XCP Protocol",
            Description = "Simplified XCP master: polled SHORT_UPLOAD reads and SET_MTA+DOWNLOAD writes.",
            CreatedOn = new DateTime(2026, 7, 10),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    // masterId/slaveId are the CAN identifiers (hex, 0x prefix optional) and must differ — the
    // 0x200/0x201 values are placeholders the operator replaces with the slave's actual ids.
    public override string DefaultRawSettings => "masterId=0x200;slaveId=0x201;extendedIds=false;timeoutMs=1000";

    // address is mandatory with no sensible default, 0x0 is a placeholder to overwrite.
    public override string CreateDefaultCommParam(IVariableBase variable, IEnumerable<IVarEvent> variableEvents)
    {
        return string.Join(";",
            "address=\"0x0\"",
            "addressExtension=\"0\"",
            "direction=\"read\"",
            $"eventRef=\"{GetDefaultEventName(variableEvents)}\"");
    }

    public override void SetConfiguration()
    {
        try
        {
            settings = XcpSessionSettings.Parse(RawSettings);
            configurationError = null;
        }
        catch (ArgumentException e)
        {
            configurationError = $"Invalid XCP session settings: {e.Message}";
            Logger?.Log(LogLevel.Error, configurationError);
        }
    }

    /// <summary>Injected by the hosting CAN driver while its bus connection is usable.</summary>
    public void SetTransmitter(Func<CanFrame, CancellationToken, Task>? frameTransmitter)
    {
        transmitter = frameTransmitter;
    }

    #region Protocol variables

    // Without events only write-only variables can be configured (polled reads need an eventRef).
    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, string commParams, bool isCommunicated)
    {
        return Build(variable, null, commParams, isCommunicated);
    }

    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IVarEvent variableEvent, string id)
    {
        return Build(variable, [variableEvent], $"address=\"{id}\";eventRef=\"{variableEvent.Name}\"", true);
    }

    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IEnumerable<IVarEvent> variableEvents,
        string commParams, bool isCommunicated)
    {
        return Build(variable, variableEvents, commParams, isCommunicated);
    }

    private XcpProtocolVariable? Build(IVariableBase variable, IEnumerable<IVarEvent>? variableEvents, string commParams,
        bool isCommunicated)
    {
        try
        {
            return new XcpProtocolVariable
            {
                Variable = variable,
                IsCommunicated = isCommunicated,
                ProtocolVariableSpecification = XcpVariableSpecification.Create(commParams, variableEvents, variable)
            };
        }
        catch (Exception e)
        {
            Logger?.Log(LogLevel.Warn, $"Protocol variable specification for variable {variable.Name} could not be created ({e.Message}).");
            return null;
        }
    }

    #endregion

    #region Protocol control

    public override Task StartAsync(CancellationToken ct = default)
    {
        if (!IsEnabled)
        {
            SetState(CommunicationState.Disabled);
            return Task.CompletedTask;
        }

        if (configurationError != null)
        {
            SetState(CommunicationState.Faulted, configurationError);
            return Task.CompletedTask;
        }

        if (State == CommunicationState.Running || runTask is { IsCompleted: false })
        {
            return Task.CompletedTask;
        }

        SetState(CommunicationState.Starting);
        exitRequested = false;
        runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        runTask = RunLoopAsync(runCts.Token);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken ct = default)
    {
        SetState(CommunicationState.Stopping);
        exitRequested = true;

        if (runCts != null)
        {
            await runCts.CancelAsync();
        }

        if (runTask != null)
        {
            try
            {
                await runTask.WaitAsync(TimeSpan.FromSeconds(5), ct);
            }
            catch (OperationCanceledException)
            {
            }
            catch (TimeoutException)
            {
                Logger?.Log(LogLevel.Warn, "XCP protocol did not stop before timeout.");
            }
        }

        runCts?.Dispose();
        runCts = null;
        runTask = null;
        SetState(CommunicationState.Stopped);
    }

    public override void Dispose()
    {
        runCts?.Dispose();
    }

    #endregion

    #region Session run loop

    private async Task RunLoopAsync(CancellationToken ct)
    {
        string? stopMessage = null;
        try
        {
            while (!ct.IsCancellationRequested && !exitRequested)
            {
                try
                {
                    await ConnectSessionAsync(ct);
                    await PollLoopAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (XcpUnsupportedSlaveException e)
                {
                    // Reconnecting cannot fix an incompatible slave — stop with a clear error.
                    stopMessage = e.Message;
                    Logger?.Log(LogLevel.Error, $"XCP: {e.Message}");
                    SetState(CommunicationState.Faulted, e.Message);
                    return;
                }
                catch (Exception e)
                {
                    Logger?.Log(LogLevel.Warn, $"XCP session failed: {e.Message} Reconnecting in {ReconnectDelayMs} ms.");
                    SetState(CommunicationState.Faulted, $"XCP session failed: {e.Message}");
                    await Task.Delay(ReconnectDelayMs, ct);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested || exitRequested)
        {
            // Normal stop.
        }
        finally
        {
            await TryDisconnectAsync();
            master = null;
            if (State != CommunicationState.Faulted)
            {
                SetState(CommunicationState.Stopped, stopMessage);
            }
            exitRequested = false;
        }
    }

    private async Task ConnectSessionAsync(CancellationToken ct)
    {
        var frameTransmitter = transmitter
                               ?? throw new XcpProtocolException("CAN transport is not available (no transmitter injected by the driver).");

        var session = new XcpMaster(Logger)
        {
            TimeoutMs = settings.TimeoutMs,
            // CAN framing: the XCP packet goes directly into the data field of a frame with the
            // configured master id; outgoing DLC is always padded to 8 bytes.
            Transmitter = (packet, token) => frameTransmitter(
                new CanFrame(settings.MasterId, PadToClassicCanDlc(packet), settings.IsExtendedId), token)
        };
        master = session;

        var status = await session.ConnectAsync(ct);
        var connectInfo = session.ConnectInfo!;

        var message = session.WritesAllowed
            ? $"Connected to XCP slave 0x{settings.SlaveId:X} ({(connectInfo.IsBigEndian ? "big" : "little")}-endian)."
            : status.IsCalibrationProtected
                ? "Connected. Calibration is protected — requires seed & key; writes are disabled."
                : "Connected. The slave has no calibration resource; writes are disabled.";

        Logger?.Log(LogLevel.Info, $"XCP: {message}");
        SetState(CommunicationState.Running, message);
    }

    private async Task TryDisconnectAsync()
    {
        var session = master;
        if (session is not { IsConnected: true } || transmitter == null)
        {
            return;
        }

        try
        {
            // Best effort with its own short budget — the run token is already cancelled here.
            await session.DisconnectAsync(CancellationToken.None).WaitAsync(TimeSpan.FromMilliseconds(1500));
        }
        catch (Exception e)
        {
            Logger?.Log(LogLevel.Warn, $"XCP: DISCONNECT on stop failed ({e.Message}).");
        }
    }

    private static byte[] PadToClassicCanDlc(byte[] packet)
    {
        if (packet.Length >= 8)
        {
            return packet;
        }

        var padded = new byte[8];
        packet.CopyTo(padded, 0);
        return padded;
    }

    #endregion

    #region Polling

    private sealed class PollEntry
    {
        public required XcpProtocolVariable ProtocolVariable { get; init; }
        public required XcpVariableSpecification Spec { get; init; }
        public required ScalarVariable Scalar { get; init; }
        public required long IntervalMs { get; init; }
        public long NextDueMs { get; set; }
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        var entries = BuildPollEntries();
        if (entries.Count == 0)
        {
            Logger?.Log(LogLevel.Info, "XCP: no pollable variables configured; serving writes only.");
            await Task.Delay(Timeout.Infinite, ct);
            return;
        }

        var consecutiveBusFailures = 0;
        while (!ct.IsCancellationRequested && !exitRequested)
        {
            var now = Environment.TickCount64;
            var anyDue = false;

            foreach (var entry in entries)
            {
                if (entry.NextDueMs > now)
                {
                    continue;
                }

                anyDue = true;
                try
                {
                    await PollVariableAsync(entry, ct);
                    consecutiveBusFailures = 0;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e) when (e is XcpErrorException or XcpTimeoutException or XcpProtocolException)
                {
                    consecutiveBusFailures++;
                    Logger?.Log(LogLevel.Warn, $"XCP: poll of '{entry.Scalar.Name}' failed: {e.Message}");
                    SetState(CommunicationState.Running, $"Poll of '{entry.Scalar.Name}' failed: {e.Message}");

                    if (consecutiveBusFailures >= MaxConsecutiveBusFailures)
                    {
                        throw new XcpProtocolException(
                            $"{consecutiveBusFailures} consecutive poll failures (last: {e.Message}).");
                    }
                }

                // Re-anchor to now: after a stall the missed cycles are skipped instead of bursting.
                entry.NextDueMs = Environment.TickCount64 + entry.IntervalMs;
            }

            if (!anyDue)
            {
                var nextDueIn = entries.Min(e => e.NextDueMs) - Environment.TickCount64;
                var delay = (int)Math.Clamp(nextDueIn, 1, SchedulerIdleMs);
                await Task.Delay(delay, ct);
            }
        }
    }

    private List<PollEntry> BuildPollEntries()
    {
        var entries = new List<PollEntry>();
        var now = Environment.TickCount64;

        foreach (var protocolVariable in Variables)
        {
            if (protocolVariable is not XcpProtocolVariable xcpVariable ||
                !xcpVariable.IsCommunicated ||
                xcpVariable.ProtocolVariableSpecification is not XcpVariableSpecification spec ||
                spec.Direction == CommDirection.Write)
            {
                continue;
            }

            if (xcpVariable.Variable is not ScalarVariable scalarVariable)
            {
                Logger?.Log(LogLevel.Warn, $"XCP: variable '{xcpVariable.Variable.Name}' is not scalar; not polled.");
                continue;
            }

            if (spec.VariableEvent is not PeriodicVarEvent periodicEvent)
            {
                Logger?.Log(LogLevel.Warn,
                    $"XCP: variable '{scalarVariable.Name}' has no periodic event ('{spec.VariableEvent?.Name}'); not polled.");
                continue;
            }

            var intervalMs = (long)periodicEvent.Period * (int)periodicEvent.Unit;
            if (intervalMs <= 0)
            {
                Logger?.Log(LogLevel.Warn,
                    $"XCP: variable '{scalarVariable.Name}' has a non-positive poll interval; not polled.");
                continue;
            }

            entries.Add(new PollEntry
            {
                ProtocolVariable = xcpVariable,
                Spec = spec,
                Scalar = scalarVariable,
                IntervalMs = intervalMs,
                NextDueMs = now
            });
        }

        return entries;
    }

    private async Task PollVariableAsync(PollEntry entry, CancellationToken ct)
    {
        var session = master ?? throw new XcpProtocolException("Not connected to the XCP slave.");

        var bytes = await session.ReadMemoryAsync(entry.Spec.AddressExtension, entry.Spec.Address, entry.Spec.Size, ct);
        var value = session.Codec.DecodeValue(bytes, entry.Spec.DataType);

        entry.Scalar.SetValue(value);
        entry.Scalar.Timestamp = DateTime.UtcNow;

        // The operator-write path listens on this notification; the flag keeps a polled update from
        // echoing back to the ECU (NotifyValueChangedAsync awaits its handlers, so the scope holds).
        entry.ProtocolVariable.IsUpdatingFromBus = true;
        try
        {
            await entry.ProtocolVariable.NotifyValueChangedAsync();
        }
        finally
        {
            entry.ProtocolVariable.IsUpdatingFromBus = false;
        }
    }

    #endregion

    #region Operator writes (RAW phase)

    public bool CanWriteVariable(IProtocolVariable protocolVariable)
    {
        return Variables.Contains(protocolVariable) &&
               protocolVariable is XcpProtocolVariable { Variable: ScalarVariable } &&
               protocolVariable.ProtocolVariableSpecification is XcpVariableSpecification
               {
                   Direction: CommDirection.Write or CommDirection.ReadWrite
               };
    }

    public async Task WriteVariableAsync(IProtocolVariable protocolVariable, CancellationToken ct = default)
    {
        if (protocolVariable is not XcpProtocolVariable xcpVariable ||
            xcpVariable.IsUpdatingFromBus || // echo of our own poll update
            xcpVariable.Variable is not ScalarVariable scalarVariable ||
            xcpVariable.ProtocolVariableSpecification is not XcpVariableSpecification spec)
        {
            return;
        }

        var session = master;
        if (session is not { IsConnected: true })
        {
            Logger?.Log(LogLevel.Warn, $"XCP: write of '{scalarVariable.Name}' skipped — not connected.");
            return;
        }

        if (!session.WritesAllowed)
        {
            var message = $"Write of '{scalarVariable.Name}' rejected: calibration requires seed & key or is unavailable.";
            Logger?.Log(LogLevel.Warn, $"XCP: {message}");
            SetState(CommunicationState.Running, message);
            return;
        }

        try
        {
            // RAW write phase: the operator value is the raw value; the staged engineering-value
            // write (inverse conversion) will slot in here later.
            var bytes = session.Codec.EncodeValue(scalarVariable.GetValue(), spec.DataType);
            await session.WriteMemoryAsync(spec.AddressExtension, spec.Address, bytes, ct);
            Logger?.Log(LogLevel.Info,
                $"XCP: wrote '{scalarVariable.Name}' ({bytes.Length} B at 0x{spec.Address:X}).");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e) when (e is XcpErrorException or XcpTimeoutException or XcpProtocolException)
        {
            Logger?.Log(LogLevel.Warn, $"XCP: write of '{scalarVariable.Name}' failed: {e.Message}");
            SetState(CommunicationState.Running, $"Write of '{scalarVariable.Name}' failed: {e.Message}");
        }
    }

    #endregion

    #region Process received data

    public override Task AddReceivedDataToQueueAsync(IEnumerable<CanFrame> data, CancellationToken ct = default)
    {
        var session = master;
        if (session == null)
        {
            return Task.CompletedTask;
        }

        foreach (var frame in data)
        {
            // Only the configured slave id carries XCP responses for this session; everything else
            // on the bus is other traffic and is ignored.
            if (frame.CanId == settings.SlaveId && frame.IsExtended == settings.IsExtendedId && frame.Data.Length > 0)
            {
                session.OnPacketReceived(frame.Data);
            }
        }

        return Task.CompletedTask;
    }

    protected override void ProcessReceivedData(IEnumerable<CanFrame> data)
    {
        AddReceivedDataToQueueAsync(data).GetAwaiter().GetResult();
    }

    protected override Task ProcessReceivedDataAsync(IEnumerable<CanFrame> data, CancellationToken ct = default)
    {
        return AddReceivedDataToQueueAsync(data, ct);
    }

    #endregion

    #region Encode & Decode

    protected override IEnumerable<CanFrame> Encode(IEnumerable<IProtocolVariable> protocolVariables)
    {
        throw new NotSupportedException("XCP uses a request/response engine; bulk Encode is not supported.");
    }

    protected override IEnumerable<IProtocolVariable> Decode(IEnumerable<CanFrame> data)
    {
        throw new NotSupportedException("XCP uses a request/response engine; bulk Decode is not supported.");
    }

    #endregion
}
