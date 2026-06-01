using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.JsonSignalProtocol;

public class JsonSignalProtocol : ProtocolBase<string>
{
    private volatile bool exitRequested;
    private readonly EventWaitHandle waitHandle;
    private readonly ConcurrentQueue<string> receivedDataQueue;

    public JsonSignalProtocol()
    {
        waitHandle = new AutoResetEvent(false);
        receivedDataQueue = new ConcurrentQueue<string>();

        Specification = new SpecificationBase
        {
            Name = "JsonSignalProtocol",
            Label = "JSON Signal Protocol",
            Description = "Processes newline-delimited JSON signal messages into protocol variable values.",
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

    public override Task AddReceivedDataToQueueAsync(IEnumerable<string> data, CancellationToken ct = default)
    {
        foreach (var line in data)
        {
            receivedDataQueue.Enqueue(line);
        }

        waitHandle.Set();
        return Task.CompletedTask;
    }

    protected override void ProcessReceivedData(IEnumerable<string> data)
    {
        foreach (var line in data)
        {
            var protocolVariable = ApplyMessage(line);
            protocolVariable?.NotifyValueChanged();
        }
    }

    protected override async Task ProcessReceivedDataAsync(IEnumerable<string> data, CancellationToken ct = default)
    {
        var notifyTasks = new List<Task>();
        foreach (var line in data)
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

    protected override IEnumerable<string> Encode(IEnumerable<IProtocolVariable> protocolVariables)
    {
        throw new NotSupportedException("JSON signal protocol does not encode outgoing data.");
    }

    protected override IEnumerable<IProtocolVariable> Decode(IEnumerable<string> data)
    {
        return data
            .Select(ApplyMessage)
            .Where(variable => variable != null)
            .Cast<IProtocolVariable>();
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        await Task.Run(async () =>
        {
            IsStarted = true;
            while (!ct.IsCancellationRequested && !exitRequested)
            {
                if (receivedDataQueue.Count > 0)
                {
                    receivedDataQueue.TryDequeue(out var line);
                    await ProcessReceivedDataAsync(new List<string> { line! }, ct);
                }
                else
                {
                    waitHandle.WaitOne();
                }
            }

            IsStarted = false;
        }, ct);
        exitRequested = false;
    }

    private IProtocolVariable? ApplyMessage(string line)
    {
        if (!TryParseMessage(line, out var signalName, out var value))
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
            protocolVariable.Variable.Timestamp = DateTime.UtcNow;
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

    private static bool TryParseMessage(string line, out string signalName, out JsonElement value)
    {
        signalName = string.Empty;
        value = default;

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
            return !string.IsNullOrWhiteSpace(signalName);
        }
        catch (JsonException)
        {
            return false;
        }
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
