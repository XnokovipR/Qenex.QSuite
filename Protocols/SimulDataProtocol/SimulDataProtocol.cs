using System.Diagnostics;
using System.Reflection;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.SimulDataProtocol;

/// <summary>
/// Simulation source: generates five signal shapes (staircase sine, noisy staircase sine and
/// three mean-reverting random walks) directly into its variables. Each variable is generated
/// in the period of its periodic event, mirroring the poll scheduling of the Modbus/XCP
/// protocols; the hosting SimDataDriver only starts and stops the protocol.
/// </summary>
public class SimulDataProtocol : ProtocolBase<int>
{
    private const int SchedulerIdleMs = 50;

    private volatile bool exitRequested;
    private CancellationTokenSource? runCts;
    private Task? runTask;

    #region Constructors

    public SimulDataProtocol()
    {
        Specification = new SpecificationBase()
        {
            Name = "SimulDataProtocol",
            Label = "Simulation Signals",
            Description = "Generates five test signal shapes; each variable in the period of its event. Use with the Simulation driver.",
            CreatedOn = new DateTime(2021, 11, 23),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    #endregion

    #region Configuration

    public override void SetConfiguration()
    {
    }

    // "signal" picks one of the five generators (step, noisystep, walk1, walk2, walk3);
    // the generation rate comes from the referenced event.
    public override string CreateDefaultCommParam(IVariableBase variable, IEnumerable<IVarEvent> variableEvents)
    {
        return string.Join(";",
            "direction=\"read\"",
            $"eventRef=\"{GetDefaultEventName(variableEvents)}\"",
            $"signal=\"{SimulSignalCatalog.StepKey}\"",
            $"id=\"{variable.Name}\"");
    }

    #endregion

    #region Protocol variables

    // The project loader uses this overload when the commParam carries no eventRef (which can
    // happen after a source move in the Project Configurator). Throwing here made such a
    // project impossible to open; instead create the variable without an event — the protocol
    // logs a "not generated" warning at start.
    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, string commParams, bool isCommunicated)
    {
        return CreateProtocolVariable(variable, [], commParams, isCommunicated);
    }

    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IVarEvent varEvent, string id)
    {
        try
        {
            return new SimulDataProtocolVariable
            {
                Variable = variable,
                IsCommunicated = true,
                ProtocolVariableSpecification = SimulDataProtocolVariableSpecification.CreateDefault(varEvent, id)
            };
        }
        catch (Exception e)
        {
            Logger?.Log(LogLevel.Warn, $"Protocol variable specification for variable {variable.Name} could not be created ({e.Message}).");
            return null;
        }
    }

    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IEnumerable<IVarEvent> variableEvents,
        string commParams, bool isCommunicated)
    {
        try
        {
            var eventRefName = commParams.Split(';').FirstOrDefault(e => e.Contains("eventRef"));
            eventRefName = eventRefName?.Split('=')[1].Trim('"');

            var varEvent = variableEvents.FirstOrDefault(e => e.Name == eventRefName);

            return new SimulDataProtocolVariable
            {
                Variable = variable,
                IsCommunicated = isCommunicated,
                ProtocolVariableSpecification = SimulDataProtocolVariableSpecification.Create(varEvent, commParams)
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

        if (State == CommunicationState.Running || runTask is { IsCompleted: false })
        {
            return Task.CompletedTask;
        }

        SetState(CommunicationState.Starting);
        exitRequested = false;

        runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        // Task.Run: the loop must not inherit the caller's (UI) SynchronizationContext —
        // a blocked dispatcher (window drag, busy UI) would stall the whole session.
        // Capture the token now: a racing StopAsync may null runCts before the loop starts.
        var runToken = runCts.Token;
        runTask = Task.Run(() => GenerateLoopAsync(runToken));
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
                Logger?.Log(LogLevel.Warn, "Simulation protocol did not stop before timeout.");
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

    #region Signal generation

    private sealed class PollEntry
    {
        public required SimulDataProtocolVariable ProtocolVariable { get; init; }
        public required ScalarVariable Scalar { get; init; }
        public required ISimulSignal Signal { get; init; }
        public required long IntervalMs { get; init; }
        public long NextDueMs { get; set; }
    }

    private async Task GenerateLoopAsync(CancellationToken ct)
    {
        try
        {
            var entries = BuildPollEntries();
            SetState(CommunicationState.Running, $"Simulation: generating {entries.Count} signal(s).");

            if (entries.Count == 0)
            {
                Logger?.Log(LogLevel.Warn, "Simulation: no variables with a periodic event; nothing to generate.");
                await Task.Delay(Timeout.Infinite, ct);
                return;
            }

            var clock = Stopwatch.StartNew();
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
                    var sample = entry.Signal.Next(clock.Elapsed.TotalSeconds);
                    SetSampleValue(entry.Scalar, sample);
                    entry.Scalar.Timestamp = DateTime.UtcNow;
                    await entry.ProtocolVariable.NotifyValueChangedAsync();

                    // Absolute anchoring keeps the average rate exactly on the event period even
                    // though Task.Delay wakes late (Windows timer granularity); a variable that
                    // fell a whole interval behind skips the missed cycles instead of bursting.
                    entry.NextDueMs += entry.IntervalMs;
                    if (entry.NextDueMs <= Environment.TickCount64)
                    {
                        entry.NextDueMs = Environment.TickCount64 + entry.IntervalMs;
                    }
                }

                if (!anyDue)
                {
                    var nextDueIn = entries.Min(e => e.NextDueMs) - Environment.TickCount64;
                    var delay = (int)Math.Clamp(nextDueIn, 1, SchedulerIdleMs);
                    await Task.Delay(delay, ct);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested || exitRequested)
        {
            // Normal stop.
        }
        catch (Exception e)
        {
            Logger?.Log(LogLevel.Error, $"Simulation loop failed: {e.Message}");
            SetState(CommunicationState.Faulted, e.Message);
        }
    }

    private List<PollEntry> BuildPollEntries()
    {
        var entries = new List<PollEntry>();
        var now = Environment.TickCount64;

        foreach (var protocolVariable in Variables)
        {
            if (protocolVariable is not SimulDataProtocolVariable simulVariable ||
                !simulVariable.IsCommunicated ||
                simulVariable.ProtocolVariableSpecification is not SimulDataProtocolVariableSpecification spec)
            {
                continue;
            }

            if (simulVariable.Variable is not ScalarVariable scalarVariable)
            {
                Logger?.Log(LogLevel.Warn, $"Simulation: variable '{simulVariable.Variable.Name}' is not scalar; not generated.");
                continue;
            }

            if (spec.VariableEvent is not PeriodicVarEvent periodicEvent)
            {
                Logger?.Log(LogLevel.Warn,
                    $"Simulation: variable '{scalarVariable.Name}' has no periodic event ('{spec.VariableEvent?.Name}'); not generated.");
                continue;
            }

            var intervalMs = (long)periodicEvent.Period * (int)periodicEvent.Unit;
            if (intervalMs <= 0)
            {
                Logger?.Log(LogLevel.Warn,
                    $"Simulation: variable '{scalarVariable.Name}' has a non-positive period; not generated.");
                continue;
            }

            if (!SupportsValues(scalarVariable))
            {
                Logger?.Log(LogLevel.Warn,
                    $"Simulation: variable '{scalarVariable.Name}' has an unsupported value type; not generated.");
                continue;
            }

            entries.Add(new PollEntry
            {
                ProtocolVariable = simulVariable,
                Scalar = scalarVariable,
                Signal = SimulSignalCatalog.Create(ResolveSignalKey(spec, scalarVariable)),
                IntervalMs = intervalMs,
                NextDueMs = now
            });
        }

        return entries;
    }

    private string ResolveSignalKey(SimulDataProtocolVariableSpecification spec, ScalarVariable scalarVariable)
    {
        if (string.IsNullOrWhiteSpace(spec.Signal))
        {
            return SimulSignalCatalog.FallbackKey(scalarVariable);
        }

        if (!SimulSignalCatalog.IsKnown(spec.Signal))
        {
            var fallback = SimulSignalCatalog.FallbackKey(scalarVariable);
            Logger?.Log(LogLevel.Warn,
                $"Simulation: variable '{scalarVariable.Name}' has an unknown signal '{spec.Signal}'; using '{fallback}'.");
            return fallback;
        }

        return spec.Signal;
    }

    private static bool SupportsValues(ScalarVariable scalarVariable)
    {
        return scalarVariable.Values is Values<double> or Values<float> or Values<long> or Values<ulong>
            or Values<int> or Values<uint> or Values<short> or Values<ushort> or Values<byte> or Values<sbyte>;
    }

    // Values<T>.SetValue does not convert, so the generated double is rounded and clamped
    // to the variable's raw type here.
    private static void SetSampleValue(ScalarVariable scalarVariable, double sample)
    {
        switch (scalarVariable.Values)
        {
            case Values<double> doubleValues:
                doubleValues.Value = sample;
                break;
            case Values<float> floatValues:
                floatValues.Value = (float)sample;
                break;
            case Values<long> longValues:
                longValues.Value = (long)Math.Clamp(Math.Round(sample), long.MinValue, long.MaxValue);
                break;
            case Values<ulong> ulongValues:
                ulongValues.Value = (ulong)Math.Clamp(Math.Round(sample), ulong.MinValue, ulong.MaxValue);
                break;
            case Values<int> intValues:
                intValues.Value = (int)Math.Clamp(Math.Round(sample), int.MinValue, int.MaxValue);
                break;
            case Values<uint> uintValues:
                uintValues.Value = (uint)Math.Clamp(Math.Round(sample), uint.MinValue, uint.MaxValue);
                break;
            case Values<short> shortValues:
                shortValues.Value = (short)Math.Clamp(Math.Round(sample), short.MinValue, short.MaxValue);
                break;
            case Values<ushort> ushortValues:
                ushortValues.Value = (ushort)Math.Clamp(Math.Round(sample), ushort.MinValue, ushort.MaxValue);
                break;
            case Values<byte> byteValues:
                byteValues.Value = (byte)Math.Clamp(Math.Round(sample), byte.MinValue, byte.MaxValue);
                break;
            case Values<sbyte> sbyteValues:
                sbyteValues.Value = (sbyte)Math.Clamp(Math.Round(sample), sbyte.MinValue, sbyte.MaxValue);
                break;
        }
    }

    #endregion

    #region Received data & encoding (not used - the protocol is a data source)

    public override Task AddReceivedDataToQueueAsync(IEnumerable<int> data, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    protected override void ProcessReceivedData(IEnumerable<int> data)
    {
    }

    protected override Task ProcessReceivedDataAsync(IEnumerable<int> data, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    protected override IEnumerable<int> Encode(IEnumerable<IProtocolVariable> protocolVariables)
    {
        throw new NotImplementedException();
    }

    protected override IEnumerable<IProtocolVariable> Decode(IEnumerable<int> data)
    {
        throw new NotImplementedException();
    }

    #endregion
}
