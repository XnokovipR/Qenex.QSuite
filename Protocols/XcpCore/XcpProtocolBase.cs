using System.Threading.Channels;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.XcpCore;

/// <summary>
/// Transport-independent core of the simplified XCP master (ASAM MCD-1 XCP 1.1). Owns everything
/// the CAN and TCP variants share: the session run loop (connect, reconnect on failure, fault on
/// incompatible slaves), polling by SHORT_UPLOAD per variable (address/size from the module XML,
/// interval from the variable's periodic event), operator writes via SET_MTA + DOWNLOAD with echo
/// suppression, and protocol-variable creation. A transport subclass contributes only its session
/// settings, the framing of outgoing XCP packets into <typeparamref name="TFrame"/> and the
/// extraction of received packets (its <see cref="ProtocolBase{T}.AddReceivedDataToQueueAsync"/>
/// feeds <see cref="Master"/>.<see cref="XcpMaster.OnPacketReceived"/>).
/// </summary>
public abstract class XcpProtocolBase<TFrame> : ProtocolBase<TFrame>, ITransportProtocol<TFrame>, IProtocolVariableWriteProtocol
{
    private const int ReconnectDelayMs = 2000;
    private const int MaxConsecutiveBusFailures = 5;
    private const int SchedulerIdleMs = 50;

    private string? configurationError;

    private volatile XcpMaster? master;
    private volatile Func<TFrame, CancellationToken, Task>? transmitter;

    private volatile bool exitRequested;
    private CancellationTokenSource? runCts;
    private Task? runTask;

    /// <summary>Live session engine while a session is established; null otherwise. The receive
    /// path of the transport subclass forwards extracted XCP packets to it.</summary>
    protected XcpMaster? Master => master;

    /// <summary>Parses and stores the transport's session settings; throws
    /// <see cref="ArgumentException"/> on invalid input (reported as a configuration fault).</summary>
    protected abstract void ApplyConfiguration(string rawSettings);

    /// <summary>Response timeout per command, from the parsed session settings.</summary>
    protected abstract int SessionTimeoutMs { get; }

    /// <summary>Transport name for diagnostics, e.g. "CAN" or "TCP".</summary>
    protected abstract string TransportName { get; }

    /// <summary>How the connected slave is called in the success log message.</summary>
    protected abstract string SlaveDescription { get; }

    /// <summary>
    /// Builds the per-session packet transmitter: wraps one outgoing XCP packet into the
    /// transport's framing and sends it through <paramref name="frameTransmitter"/>. Called once
    /// on every session (re)connect, so per-session framing state resets here.
    /// </summary>
    protected abstract Func<byte[], CancellationToken, Task> CreatePacketTransmitter(
        Func<TFrame, CancellationToken, Task> frameTransmitter);

    // Without events only write-only variables can be configured (polled reads need an eventRef).
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
            ApplyConfiguration(RawSettings);
            configurationError = null;
        }
        catch (ArgumentException e)
        {
            configurationError = $"Invalid XCP session settings: {e.Message}";
            Logger?.Log(LogLevel.Error, configurationError);
        }
    }

    /// <summary>Injected by the hosting driver while its connection is usable.</summary>
    public void SetTransmitter(Func<TFrame, CancellationToken, Task>? frameTransmitter)
    {
        transmitter = frameTransmitter;
    }

    #region Protocol variables

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
                ProtocolVariableSpecification = XcpVariableSpecification.Create(commParams, variableEvents, variable, Logger)
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
                    await RunSessionAsync(ct);
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
                               ?? throw new XcpProtocolException(
                                   $"{TransportName} transport is not available (no transmitter injected by the driver).");

        var session = new XcpMaster(Logger)
        {
            TimeoutMs = SessionTimeoutMs,
            Transmitter = CreatePacketTransmitter(frameTransmitter)
        };
        master = session;

        var status = await session.ConnectAsync(ct);
        var connectInfo = session.ConnectInfo!;

        var message = session.WritesAllowed
            ? $"Connected to {SlaveDescription} ({(connectInfo.IsBigEndian ? "big" : "little")}-endian)."
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

    #endregion

    #region Session body (DAQ + polling)

    /// <summary>
    /// Runs one connected session: sets up DAQ for variables bound to DAQ events (D1–D6; a failed
    /// setup falls back to polling for everything), then serves polling and the DAQ DTO stream
    /// concurrently until the session dies or the protocol stops.
    /// </summary>
    private async Task RunSessionAsync(CancellationToken ct)
    {
        var daqSession = await TrySetupDaqAsync(ct);
        if (daqSession == null)
        {
            await PollLoopAsync(null, ct);
            return;
        }

        var consumer = ConsumeDaqDtosAsync(daqSession, ct);
        try
        {
            await PollLoopAsync(daqSession.VariablesInDaq, ct);
        }
        finally
        {
            if (master is { } session)
            {
                session.DaqDtoReceived = null;
            }

            daqSession.Dtos.Writer.TryComplete();
            try
            {
                await consumer;
            }
            catch (OperationCanceledException)
            {
            }
        }
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

    private async Task PollLoopAsync(IReadOnlySet<IProtocolVariable>? daqServedVariables, CancellationToken ct)
    {
        var entries = BuildPollEntries(daqServedVariables);
        if (entries.Count == 0)
        {
            Logger?.Log(LogLevel.Info, daqServedVariables == null
                ? "XCP: no pollable variables configured; serving writes only."
                : "XCP: all read variables are served by DAQ; polling is idle.");

            // Keep the session alive but notice a dead transport (the driver nulls the
            // transmitter on connection loss) — otherwise a DAQ-only session would never reconnect.
            while (!ct.IsCancellationRequested && !exitRequested)
            {
                if (transmitter == null)
                {
                    throw new XcpProtocolException($"{TransportName} transport is no longer available.");
                }

                await Task.Delay(500, ct);
            }

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

    private List<PollEntry> BuildPollEntries(IReadOnlySet<IProtocolVariable>? daqServedVariables)
    {
        var entries = new List<PollEntry>();
        var now = Environment.TickCount64;

        foreach (var protocolVariable in Variables)
        {
            if (protocolVariable is not XcpProtocolVariable xcpVariable ||
                !xcpVariable.IsCommunicated ||
                xcpVariable.ProtocolVariableSpecification is not XcpVariableSpecification spec ||
                spec.Direction == CommDirection.Write ||
                daqServedVariables?.Contains(protocolVariable) == true)
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

        await ApplyBusValueAsync(entry.ProtocolVariable, entry.Scalar, value, DateTime.UtcNow);
    }

    private static async Task ApplyBusValueAsync(XcpProtocolVariable protocolVariable, ScalarVariable scalar,
        object value, DateTime timestampUtc)
    {
        scalar.SetValue(value);
        scalar.Timestamp = timestampUtc;

        // The operator-write path listens on this notification; the flag keeps a bus-sourced update
        // from echoing back to the ECU (NotifyValueChangedAsync awaits its handlers, so the scope holds).
        protocolVariable.IsUpdatingFromBus = true;
        try
        {
            await protocolVariable.NotifyValueChangedAsync();
        }
        finally
        {
            protocolVariable.IsUpdatingFromBus = false;
        }
    }

    #endregion

    #region DAQ (measurement, decisions D1-D6)

    private readonly record struct DaqDto(byte[] Packet, DateTime ReceivedUtc);

    private sealed record DaqTarget(XcpProtocolVariable ProtocolVariable, ScalarVariable Scalar, XcpVariableSpecification Spec);

    private sealed class DaqSessionState
    {
        public required XcpDaqDecoder Decoder { get; init; }
        public required XcpCodec Codec { get; init; }
        public required DaqTarget[][][] Targets { get; init; } // [daq list][odt][entry], mirrors the plans
        public required Channel<DaqDto> Dtos { get; init; }
        public required IReadOnlySet<IProtocolVariable> VariablesInDaq { get; init; }
    }

    /// <summary>
    /// Builds and starts the DAQ side of the session for all variables bound to DAQ events.
    /// Returns null when there is nothing to acquire or when DAQ cannot be used — then every
    /// variable stays with polling (D4: polling is the fallback), which is always reported.
    /// </summary>
    private async Task<DaqSessionState?> TrySetupDaqAsync(CancellationToken ct)
    {
        var session = master;
        if (session is not { IsConnected: true })
        {
            return null;
        }

        var channels = CollectDaqCandidates();
        if (channels.Count == 0)
        {
            return null;
        }

        var connectInfo = session.ConnectInfo!;
        if (!connectInfo.SupportsDaq)
        {
            Logger?.Log(LogLevel.Warn,
                "XCP: the slave has no DAQ resource; variables with DAQ events fall back to polling.");
            return null;
        }

        if (session.LastStatus?.IsDaqProtected == true)
        {
            Logger?.Log(LogLevel.Warn,
                "XCP: the slave protects DAQ with seed & key, which is not supported; variables with DAQ events fall back to polling.");
            return null;
        }

        try
        {
            var processor = await session.GetDaqProcessorInfoAsync(ct);
            if (!processor.HasDynamicLists)
            {
                Logger?.Log(LogLevel.Warn,
                    "XCP: the slave only supports static DAQ lists, which is not implemented; falling back to polling.");
                return null;
            }

            XcpDaqResolutionInfo? resolution = null;
            try
            {
                resolution = await session.GetDaqResolutionInfoAsync(ct);
            }
            catch (XcpErrorException e) when (e.ErrorCode == XcpErrorCode.CmdUnknown)
            {
                Logger?.Log(LogLevel.Info, "XCP: GET_DAQ_RESOLUTION_INFO not implemented by the slave; using defaults.");
            }

            await ValidateDaqChannelsAsync(session, processor, channels, ct);
            if (channels.Count == 0)
            {
                Logger?.Log(LogLevel.Warn, "XCP: no DAQ event channel survived validation; falling back to polling.");
                return null;
            }

            var includeTimestamp = resolution is { TimestampFixed: true, TimestampSize: > 0 };
            var timestampSize = includeTimestamp ? resolution!.TimestampSize : 0;
            var (plans, targets) = PackDaqLists(channels, connectInfo, processor, resolution, timestampSize);
            if (plans.Count == 0)
            {
                Logger?.Log(LogLevel.Warn, "XCP: no variable fits into a DAQ packet; falling back to polling.");
                return null;
            }

            var decoder = new XcpDaqDecoder(processor.IdentificationType, session.Codec.IsBigEndian,
                timestampSize, processor.OverloadIndicationByPid, plans, Logger);

            var variablesInDaq = targets
                .SelectMany(list => list.SelectMany(odt => odt))
                .Select(IProtocolVariable (target) => target.ProtocolVariable)
                .ToHashSet();

            var droppedDtos = 0L;
            var dtos = Channel.CreateBounded<DaqDto>(
                new BoundedChannelOptions(8192)
                {
                    SingleReader = true,
                    FullMode = BoundedChannelFullMode.DropOldest
                },
                _ =>
                {
                    // D6: overload never faults the session; dropping the oldest keeps the view live.
                    droppedDtos++;
                    if (droppedDtos == 1 || droppedDtos % 1000 == 0)
                    {
                        Logger?.Log(LogLevel.Warn,
                            $"XCP: DAQ processing cannot keep up — {droppedDtos} packets dropped so far (oldest first).");
                    }
                });

            // Wired before START_STOP_SYNCH — the slave starts streaming the instant it acks, and
            // nothing may fall into the gap. (Absolute-PID slaves still drop DTOs until the FIRST_PIDs
            // arrive right below; those are counted by the decoder.)
            session.DaqDtoReceived = packet => dtos.Writer.TryWrite(new DaqDto(packet, DateTime.UtcNow));

            var firstPids = await session.ConfigureAndStartDaqAsync(plans, includeTimestamp, ct);
            decoder.SetFirstPids(firstPids);

            Logger?.Log(LogLevel.Info,
                $"XCP: DAQ started — {plans.Count} list(s) on event channel(s) " +
                $"{string.Join(", ", plans.Select(p => p.EventChannel))}, {variablesInDaq.Count} variable(s).");

            return new DaqSessionState
            {
                Decoder = decoder,
                Codec = session.Codec,
                Targets = targets,
                Dtos = dtos,
                VariablesInDaq = variablesInDaq
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e) when (e is XcpErrorException or XcpTimeoutException or XcpProtocolException)
        {
            session.DaqDtoReceived = null;
            Logger?.Log(LogLevel.Warn, $"XCP: DAQ setup failed ({e.Message}); falling back to polling for all variables.");
            return null;
        }
    }

    /// <summary>Read variables bound to a DAQ event, grouped by ECU event channel (stable order).</summary>
    private SortedDictionary<ushort, List<DaqTarget>> CollectDaqCandidates()
    {
        var channels = new SortedDictionary<ushort, List<DaqTarget>>();

        foreach (var protocolVariable in Variables)
        {
            if (protocolVariable is not XcpProtocolVariable xcpVariable ||
                !xcpVariable.IsCommunicated ||
                xcpVariable.ProtocolVariableSpecification is not XcpVariableSpecification spec ||
                spec.DaqEventChannel is not { } channel ||
                spec.Direction == CommDirection.Write)
            {
                continue;
            }

            if (xcpVariable.Variable is not ScalarVariable scalarVariable)
            {
                Logger?.Log(LogLevel.Warn, $"XCP: variable '{xcpVariable.Variable.Name}' is not scalar; not acquired via DAQ.");
                continue;
            }

            if (!channels.TryGetValue(channel, out var targets))
            {
                channels[channel] = targets = [];
            }

            targets.Add(new DaqTarget(xcpVariable, scalarVariable, spec));
        }

        return channels;
    }

    /// <summary>
    /// D3: every used channel is validated via GET_DAQ_EVENT_INFO — the ECU name is logged and the
    /// event's configured period is compared with the ECU's nominal TIMECYCLE (mismatch = warning
    /// with both values). Channels the slave rejects are removed and their variables fall back to
    /// polling. Slaves without GET_DAQ_EVENT_INFO skip validation with a notice.
    /// </summary>
    private async Task ValidateDaqChannelsAsync(XcpMaster session, XcpDaqProcessorInfo processor,
        SortedDictionary<ushort, List<DaqTarget>> channels, CancellationToken ct)
    {
        foreach (var channel in channels.Keys.ToList())
        {
            if (processor.MaxEventChannel > 0 && channel >= processor.MaxEventChannel)
            {
                Logger?.Log(LogLevel.Warn,
                    $"XCP: DAQ event channel {channel} is out of the slave's range (0..{processor.MaxEventChannel - 1}); " +
                    $"its variables ({DescribeTargets(channels[channel])}) fall back to polling.");
                channels.Remove(channel);
                continue;
            }

            XcpDaqEventInfo info;
            string ecuName;
            try
            {
                (info, ecuName) = await session.GetDaqEventInfoAsync(channel, ct);
            }
            catch (XcpErrorException e) when (e.ErrorCode == XcpErrorCode.CmdUnknown)
            {
                Logger?.Log(LogLevel.Info,
                    "XCP: GET_DAQ_EVENT_INFO not implemented by the slave; DAQ event channels are used unvalidated.");
                return;
            }
            catch (XcpErrorException e) when (e.ErrorCode == XcpErrorCode.OutOfRange)
            {
                Logger?.Log(LogLevel.Warn,
                    $"XCP: the slave rejected DAQ event channel {channel} (ERR_OUT_OF_RANGE); " +
                    $"its variables ({DescribeTargets(channels[channel])}) fall back to polling.");
                channels.Remove(channel);
                continue;
            }

            if (!info.SupportsDaq)
            {
                Logger?.Log(LogLevel.Warn,
                    $"XCP: event channel {channel} ('{ecuName}') does not support the DAQ direction; " +
                    $"its variables ({DescribeTargets(channels[channel])}) fall back to polling.");
                channels.Remove(channel);
                continue;
            }

            Logger?.Log(LogLevel.Info,
                $"XCP: DAQ event channel {channel} = '{ecuName}'" +
                (info.CycleTimeMs is { } cycle ? $", nominal cycle {cycle:0.###} ms." : ", sporadic (no nominal cycle)."));

            WarnOnPeriodMismatch(channels[channel], channel, ecuName, info);
        }
    }

    private void WarnOnPeriodMismatch(List<DaqTarget> targets, ushort channel, string ecuName, XcpDaqEventInfo info)
    {
        if (info.CycleTimeMs is not { } cycleMs)
        {
            return;
        }

        foreach (var target in targets)
        {
            if (target.Spec.VariableEvent is not PeriodicVarEvent periodicEvent)
            {
                continue;
            }

            var configuredMs = (double)periodicEvent.Period * (int)periodicEvent.Unit;
            if (configuredMs > 0 && Math.Abs(configuredMs - cycleMs) > cycleMs * 0.5)
            {
                Logger?.Log(LogLevel.Warn,
                    $"XCP: event '{periodicEvent.Name}' is configured with {configuredMs:0.###} ms but ECU channel {channel} " +
                    $"('{ecuName}') reports a nominal cycle of {cycleMs:0.###} ms — the ECU timing wins; " +
                    "adjust the event period if the difference is unintended.");
            }
        }
    }

    private static string DescribeTargets(List<DaqTarget> targets)
    {
        return string.Join(", ", targets.Select(t => $"'{t.Scalar.Name}'"));
    }

    /// <summary>
    /// Packs each channel's entries into ODTs (first-fit, configuration order). ODT capacity is
    /// MAX_DTO minus the identification field (and the timestamp in ODT 0); entry size is further
    /// capped by MAX_ODT_ENTRY_SIZE_DAQ. Oversized variables are skipped with a warning.
    /// </summary>
    private (List<XcpDaqListPlan> Plans, DaqTarget[][][] Targets) PackDaqLists(
        SortedDictionary<ushort, List<DaqTarget>> channels, XcpConnectResponse connectInfo,
        XcpDaqProcessorInfo processor, XcpDaqResolutionInfo? resolution, int timestampSize)
    {
        var maxEntrySize = resolution is { MaxOdtEntrySizeDaq: > 0 } ? resolution.MaxOdtEntrySizeDaq : byte.MaxValue;
        var headerSize = processor.DtoHeaderSize;

        var plans = new List<XcpDaqListPlan>();
        var targets = new List<DaqTarget[][]>();

        foreach (var (channel, channelTargets) in channels)
        {
            var odts = new List<List<XcpDaqEntryPlan>>();
            var odtTargets = new List<List<DaqTarget>>();

            foreach (var target in channelTargets)
            {
                var size = target.Spec.Size;
                var capacityNext = connectInfo.MaxDto - headerSize - (odts.Count == 0 ? timestampSize : 0);
                if (size > maxEntrySize || size > capacityNext)
                {
                    Logger?.Log(LogLevel.Warn,
                        $"XCP: variable '{target.Scalar.Name}' ({size} B) does not fit a DAQ packet " +
                        $"(MAX_ODT_ENTRY_SIZE {maxEntrySize}, free ODT capacity {capacityNext}); it falls back to polling.");
                    continue;
                }

                var capacityCurrent = connectInfo.MaxDto - headerSize - (odts.Count <= 1 ? timestampSize : 0);
                if (odts.Count == 0 ||
                    odts[^1].Sum(e => (int)e.Size) + size > capacityCurrent ||
                    odts[^1].Count == byte.MaxValue)
                {
                    odts.Add([]);
                    odtTargets.Add([]);
                }

                odts[^1].Add(new XcpDaqEntryPlan((byte)size, target.Spec.AddressExtension, target.Spec.Address));
                odtTargets[^1].Add(target);
            }

            if (odts.Count == 0)
            {
                continue;
            }

            if (odts.Count > 0xFC)
            {
                throw new XcpProtocolException(
                    $"DAQ list for event channel {channel} needs {odts.Count} ODTs; at most 252 are addressable.");
            }

            plans.Add(new XcpDaqListPlan(channel, odts.Select(IReadOnlyList<XcpDaqEntryPlan> (o) => o).ToList()));
            targets.Add(odtTargets.Select(o => o.ToArray()).ToArray());
        }

        return (plans, targets.ToArray());
    }

    /// <summary>
    /// Consumes queued DAQ packets and pushes decoded values into their variables. Timestamp is
    /// the PC receive time (D5 stage 4a); the ECU timestamp in ODT 0 is skipped by the decoder.
    /// </summary>
    private async Task ConsumeDaqDtosAsync(DaqSessionState daq, CancellationToken ct)
    {
        var decoded = new List<XcpDaqDecoder.DecodedEntry>(capacity: 16);
        var decodeFailures = 0L;

        await foreach (var dto in daq.Dtos.Reader.ReadAllAsync(ct))
        {
            if (!daq.Decoder.TryDecode(dto.Packet, decoded))
            {
                continue;
            }

            foreach (var entry in decoded)
            {
                var target = daq.Targets[entry.ListIndex][entry.OdtIndex][entry.EntryIndex];
                object value;
                try
                {
                    value = daq.Codec.DecodeValue(dto.Packet.AsSpan(entry.DataOffset, entry.Size), target.Spec.DataType);
                }
                catch (XcpProtocolException e)
                {
                    decodeFailures++;
                    if (decodeFailures == 1 || decodeFailures % 1000 == 0)
                    {
                        Logger?.Log(LogLevel.Warn,
                            $"XCP: DAQ value for '{target.Scalar.Name}' could not be decoded ({e.Message}); {decodeFailures} failures so far.");
                    }

                    continue;
                }

                await ApplyBusValueAsync(target.ProtocolVariable, target.Scalar, value, dto.ReceivedUtc);
            }
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

    protected override void ProcessReceivedData(IEnumerable<TFrame> data)
    {
        AddReceivedDataToQueueAsync(data).GetAwaiter().GetResult();
    }

    protected override Task ProcessReceivedDataAsync(IEnumerable<TFrame> data, CancellationToken ct = default)
    {
        return AddReceivedDataToQueueAsync(data, ct);
    }

    #endregion

    #region Encode & Decode

    protected override IEnumerable<TFrame> Encode(IEnumerable<IProtocolVariable> protocolVariables)
    {
        throw new NotSupportedException("XCP uses a request/response engine; bulk Encode is not supported.");
    }

    protected override IEnumerable<IProtocolVariable> Decode(IEnumerable<TFrame> data)
    {
        throw new NotSupportedException("XCP uses a request/response engine; bulk Decode is not supported.");
    }

    #endregion
}
