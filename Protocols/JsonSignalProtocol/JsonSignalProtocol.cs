using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.JsonSignalProtocol;

public class JsonSignalProtocol : ProtocolBase<byte[]>
{
    private volatile bool exitRequested;
    private readonly EventWaitHandle waitHandle;
    private readonly ConcurrentQueue<string> receivedDataQueue;
    // Byte→text decoding and line splitting are this protocol's job — the hosting driver only
    // transports byte chunks. The framer is stateful (partial lines, split UTF-8 sequences).
    private readonly TextLineFramer lineFramer = new();
    private readonly Lock framerLock = new();
    private DateTime? sourceTimeBaseUtc;
    private double? lastSourceTimeSeconds;

    public JsonSignalProtocol()
    {
        waitHandle = new AutoResetEvent(false);
        receivedDataQueue = new ConcurrentQueue<string>();

        Specification = new SpecificationBase
        {
            Name = "JsonSignalProtocol",
            Label = "JSON Signal Protocol",
            Description = "Decodes newline-delimited JSON signal messages from raw byte chunks into protocol variable values.",
            CreatedOn = new DateTime(2026, 6, 1),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    public override void SetConfiguration()
    {
    }

    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, string commParams, bool isCommunicated)
    {
        return new JsonSignalProtocolVariable
        {
            Variable = variable,
            IsCommunicated = isCommunicated,
            ProtocolVariableSpecification = JsonSignalProtocolVariableSpecification.Create(commParams, variable.Name)
        };
    }

    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IVarEvent variableEvent, string id)
    {
        return CreateProtocolVariable(variable, $"id=\"{id}\"", true);
    }

    public override IProtocolVariable? CreateProtocolVariable(
        IVariableBase variable,
        IEnumerable<IVarEvent> variableEvents,
        string commParams,
        bool isCommunicated)
    {
        return CreateProtocolVariable(variable, commParams, isCommunicated);
    }

    public override Task StartAsync(CancellationToken ct = default)
    {
        if (!IsEnabled) return Task.CompletedTask;
        exitRequested = false;
        sourceTimeBaseUtc = null;
        lastSourceTimeSeconds = null;
        lock (framerLock)
        {
            lineFramer.Reset();
        }
        _ = RunLoopAsync(ct);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken ct = default)
    {
        exitRequested = true;
        waitHandle.Set();
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        waitHandle.Dispose();
    }

    public override Task AddReceivedDataToQueueAsync(IEnumerable<byte[]> data, CancellationToken ct = default)
    {
        foreach (var line in ToLines(data))
        {
            receivedDataQueue.Enqueue(line);
        }

        waitHandle.Set();
        return Task.CompletedTask;
    }

    protected override void ProcessReceivedData(IEnumerable<byte[]> data)
    {
        foreach (var line in ToLines(data))
        {
            var protocolVariable = ApplyMessage(line);
            protocolVariable?.NotifyValueChanged();
        }
    }

    protected override async Task ProcessReceivedDataAsync(IEnumerable<byte[]> data, CancellationToken ct = default)
    {
        var notifyTasks = new List<Task>();
        foreach (var line in ToLines(data))
        {
            ct.ThrowIfCancellationRequested();
            var protocolVariable = ApplyMessage(line);
            if (protocolVariable != null)
            {
                notifyTasks.Add(protocolVariable.NotifyValueChangedAsync());
            }
        }

        await Task.WhenAll(notifyTasks);
    }

    protected override IEnumerable<byte[]> Encode(IEnumerable<IProtocolVariable> protocolVariables)
    {
        throw new NotSupportedException("JSON signal protocol does not encode outgoing data.");
    }

    protected override IEnumerable<IProtocolVariable> Decode(IEnumerable<byte[]> data)
    {
        return ToLines(data)
            .Select(ApplyMessage)
            .Where(variable => variable != null)
            .Cast<IProtocolVariable>();
    }

    private List<string> ToLines(IEnumerable<byte[]> chunks)
    {
        var lines = new List<string>();
        lock (framerLock)
        {
            foreach (var chunk in chunks)
            {
                lines.AddRange(lineFramer.Append(chunk));
            }
        }

        return lines;
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        await Task.Run(async () =>
        {
            SetState(CommunicationState.Running);
            while (!ct.IsCancellationRequested && !exitRequested)
            {
                if (receivedDataQueue.Count > 0)
                {
                    receivedDataQueue.TryDequeue(out var line);
                    var protocolVariable = ApplyMessage(line!);
                    if (protocolVariable != null)
                    {
                        await protocolVariable.NotifyValueChangedAsync();
                    }
                }
                else
                {
                    waitHandle.WaitOne();
                }
            }

            SetState(CommunicationState.Stopped);
        }, ct);
        exitRequested = false;
    }

    private IProtocolVariable? ApplyMessage(string line)
    {
        if (!TryParseMessage(line, out var signalName, out var value, out var sourceTimeSeconds))
        {
            return null;
        }

        var protocolVariable = FindProtocolVariable(signalName);
        if (protocolVariable == null)
        {
            return null;
        }

        try
        {
            protocolVariable.Variable.Timestamp = GetTimestamp(sourceTimeSeconds);
            protocolVariable.Variable.SetValue(ConvertValue(value, protocolVariable.Variable));
            return protocolVariable;
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or InvalidOperationException or OverflowException or ArgumentException)
        {
            return null;
        }
    }

    private IProtocolVariable? FindProtocolVariable(string signalName)
    {
        return Variables.FirstOrDefault(variable =>
            variable.IsCommunicated
            && variable.ProtocolVariableSpecification is JsonSignalProtocolVariableSpecification spec
            && string.Equals(spec.SignalName, signalName, StringComparison.OrdinalIgnoreCase));
    }

    private bool TryParseMessage(
        string line,
        out string signalName,
        out JsonElement value,
        out double? sourceTimeSeconds)
    {
        signalName = string.Empty;
        value = default;
        sourceTimeSeconds = null;

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (!root.TryGetProperty("name", out var nameElement) ||
                !root.TryGetProperty("value", out var valueElement))
            {
                return false;
            }

            signalName = nameElement.GetString() ?? string.Empty;
            value = valueElement.Clone();
            if (root.TryGetProperty("t", out var sourceTimeElement)
                && TryReadSourceTimeSeconds(sourceTimeElement, out var parsedSourceTimeSeconds))
            {
                sourceTimeSeconds = parsedSourceTimeSeconds;
            }

            return !string.IsNullOrWhiteSpace(signalName);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private DateTime GetTimestamp(double? sourceTimeSeconds)
    {
        if (sourceTimeSeconds == null)
        {
            return DateTime.UtcNow;
        }

        var sourceTime = sourceTimeSeconds.Value;
        if (sourceTimeBaseUtc == null
            || (lastSourceTimeSeconds != null && sourceTime < lastSourceTimeSeconds.Value))
        {
            sourceTimeBaseUtc = DateTime.UtcNow - TimeSpan.FromSeconds(sourceTime);
        }

        lastSourceTimeSeconds = sourceTime;
        return DateTime.SpecifyKind(sourceTimeBaseUtc.Value + TimeSpan.FromSeconds(sourceTime), DateTimeKind.Utc);
    }

    private static bool TryReadSourceTimeSeconds(JsonElement element, out double sourceTimeSeconds)
    {
        sourceTimeSeconds = 0;
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                if (!element.TryGetDouble(out sourceTimeSeconds))
                {
                    return false;
                }

                break;
            case JsonValueKind.String:
                if (!double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out sourceTimeSeconds))
                {
                    return false;
                }

                break;
            default:
                return false;
        }

        return !double.IsNaN(sourceTimeSeconds)
               && !double.IsInfinity(sourceTimeSeconds)
               && sourceTimeSeconds >= 0;
    }

    private static object ConvertValue(JsonElement value, IVariableBase variable)
    {
        var targetType = GetTargetType(variable);
        if (targetType == typeof(string))
        {
            return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
        }

        if (targetType == typeof(bool))
        {
            return value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False
                ? value.GetBoolean()
                : bool.Parse(value.ToString());
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value.ToString(), ignoreCase: true);
        }

        var numericValue = value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : double.Parse(value.ToString(), CultureInfo.InvariantCulture);
        return Convert.ChangeType(numericValue, targetType, CultureInfo.InvariantCulture);
    }

    private static Type GetTargetType(IVariableBase variable)
    {
        if (variable is StringVariable)
        {
            return typeof(string);
        }

        return variable.GetValue()?.GetType() ?? typeof(double);
    }
}
