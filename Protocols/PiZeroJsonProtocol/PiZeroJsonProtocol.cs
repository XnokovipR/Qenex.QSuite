using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.PiZeroJsonProtocol;

public class PiZeroJsonProtocol : ProtocolBase<byte[]>, ITransportProtocol<byte[]>, IProtocolVariableWriteProtocol
{
    private readonly object timestampLock = new();
    // Byte→text decoding and line splitting are this protocol's job — the hosting driver only
    // transports byte chunks. The framer is stateful (partial lines, split UTF-8 sequences).
    private readonly TextLineFramer lineFramer = new();
    private readonly Lock framerLock = new();
    private volatile Func<byte[], CancellationToken, Task>? transmitter;
    private DateTime? remoteTimestampBaseUtc;
    private double? firstRemoteSeconds;
    private double? lastRemoteSeconds;

    public PiZeroJsonProtocol()
    {
        Specification = new SpecificationBase
        {
            Name = "PiZeroJsonProtocol",
            Label = "Pi Zero JSON Protocol",
            Description = "JSON line protocol for Raspberry Pi Zero signal data and commands.",
            CreatedOn = new DateTime(2026, 5, 31),
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
        return new PiZeroJsonProtocolVariable
        {
            Variable = variable,
            IsCommunicated = isCommunicated,
            ProtocolVariableSpecification = PiZeroJsonProtocolVariableSpecification.Create(commParams)
        };
    }

    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IVarEvent variableEvent, string id)
    {
        return CreateProtocolVariable(variable, $"direction=\"read\";id=\"{id}\";multiplier=\"1\"", true);
    }

    public override IProtocolVariable? CreateProtocolVariable(
        IVariableBase variable,
        IEnumerable<IVarEvent> variableEvents,
        string commParams,
        bool isCommunicated)
    {
        var parameters = ParseParameters(commParams);
        IVarEvent? variableEvent = null;
        if (parameters.TryGetValue("eventRef", out var eventRef))
        {
            variableEvent = variableEvents.FirstOrDefault(e => e.Name == eventRef);
        }

        return new PiZeroJsonProtocolVariable
        {
            Variable = variable,
            IsCommunicated = isCommunicated,
            ProtocolVariableSpecification = PiZeroJsonProtocolVariableSpecification.Create(variableEvent, commParams)
        };
    }

    /// <summary>Injected by the hosting driver (TCP client) while its connection is usable.</summary>
    public void SetTransmitter(Func<byte[], CancellationToken, Task>? byteTransmitter)
    {
        transmitter = byteTransmitter;
    }

    public override Task StartAsync(CancellationToken ct = default)
    {
        ResetRemoteTimestampMapping();
        lock (framerLock)
        {
            lineFramer.Reset();
        }
        SetState(IsEnabled ? CommunicationState.Running : CommunicationState.Disabled);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken ct = default)
    {
        SetState(CommunicationState.Stopped);
        ResetRemoteTimestampMapping();
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
    }

    public override Task AddReceivedDataToQueueAsync(IEnumerable<byte[]> data, CancellationToken ct = default)
    {
        return State == CommunicationState.Running ? ProcessReceivedDataAsync(data, ct) : Task.CompletedTask;
    }

    public bool CanWriteVariable(IProtocolVariable protocolVariable)
    {
        return Variables.Contains(protocolVariable) && FindCommandVariable(protocolVariable) != null;
    }

    /// <summary>Operator write: serializes the "set" command as one JSON line and transmits it.</summary>
    public async Task WriteVariableAsync(IProtocolVariable protocolVariable, CancellationToken ct = default)
    {
        var command = EncodeCommand(protocolVariable);
        if (command == null)
        {
            return;
        }

        var sendCommand = transmitter;
        if (sendCommand == null)
        {
            Logger?.Log(LogLevel.Warn, "Pi Zero JSON protocol: command dropped — transport is not available.");
            return;
        }

        await sendCommand(System.Text.Encoding.UTF8.GetBytes(command + "\n"), ct);
    }

    private string? EncodeCommand(IProtocolVariable protocolVariable)
    {
        var commandVariable = FindCommandVariable(protocolVariable);
        if (commandVariable?.ProtocolVariableSpecification is not PiZeroJsonProtocolVariableSpecification spec)
        {
            return null;
        }

        var value = protocolVariable.Variable.GetValue();
        if (value == null)
        {
            return null;
        }

        var command = new Dictionary<string, object?>
        {
            ["cmd"] = "set",
            ["name"] = spec.Id
        };
        command[spec.Param] = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        return JsonSerializer.Serialize(command);
    }

    protected override void ProcessReceivedData(IEnumerable<byte[]> data)
    {
        foreach (var line in ToLines(data))
        {
            var protocolVariable = DecodeLine(line);
            protocolVariable?.NotifyValueChanged();
        }
    }

    protected override async Task ProcessReceivedDataAsync(IEnumerable<byte[]> data, CancellationToken ct = default)
    {
        var notifyTasks = new List<Task>();
        foreach (var line in ToLines(data))
        {
            ct.ThrowIfCancellationRequested();
            var protocolVariable = DecodeLine(line);
            if (protocolVariable != null)
            {
                notifyTasks.Add(protocolVariable.NotifyValueChangedAsync());
            }
        }

        await Task.WhenAll(notifyTasks);
    }

    protected override IEnumerable<byte[]> Encode(IEnumerable<IProtocolVariable> protocolVariables)
    {
        return protocolVariables
            .Select(EncodeCommand)
            .Where(command => command != null)
            .Select(command => System.Text.Encoding.UTF8.GetBytes(command + "\n"));
    }

    protected override IEnumerable<IProtocolVariable> Decode(IEnumerable<byte[]> data)
    {
        return ToLines(data)
            .Select(DecodeLine)
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

    private IProtocolVariable? DecodeLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (!root.TryGetProperty("name", out var nameElement)
                || !root.TryGetProperty("value", out var valueElement))
            {
                return null;
            }

            var name = nameElement.GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var protocolVariable = FindReadableVariable(name);
            if (protocolVariable == null)
            {
                return null;
            }

            protocolVariable.Variable.Timestamp = ReadTimestamp(root);
            protocolVariable.Variable.SetValue(ReadValue(valueElement, protocolVariable.Variable));
            return protocolVariable;
        }
        catch (JsonException e)
        {
            Logger?.Log(LogLevel.Warn, $"Pi Zero JSON line could not be decoded: {e.Message}");
            return null;
        }
        catch (Exception e)
        {
            Logger?.Log(LogLevel.Warn, $"Pi Zero protocol variable could not be updated: {e.Message}");
            return null;
        }
    }

    private IProtocolVariable? FindReadableVariable(string id)
    {
        return Variables.FirstOrDefault(variable =>
            variable.IsCommunicated
            && variable.ProtocolVariableSpecification is PiZeroJsonProtocolVariableSpecification spec
            && string.Equals(spec.Id, id, StringComparison.OrdinalIgnoreCase)
            && spec.Direction is CommDirection.Read or CommDirection.ReadWrite);
    }

    private IProtocolVariable? FindCommandVariable(IProtocolVariable protocolVariable)
    {
        return Variables.FirstOrDefault(variable =>
            ReferenceEquals(variable, protocolVariable)
            && variable.IsCommunicated
            && variable.ProtocolVariableSpecification is PiZeroJsonProtocolVariableSpecification spec
            && spec.Direction is CommDirection.Write or CommDirection.ReadWrite
            && !string.IsNullOrWhiteSpace(spec.Id)
            && (string.Equals(spec.Param, "A", StringComparison.Ordinal)
                || string.Equals(spec.Param, "f", StringComparison.Ordinal)));
    }

    private static object ReadValue(JsonElement valueElement, IVariableBase variable)
    {
        var targetType = variable.GetValue()?.GetType() ?? typeof(double);
        if (targetType == typeof(string))
        {
            return valueElement.ValueKind == JsonValueKind.String
                ? valueElement.GetString() ?? string.Empty
                : valueElement.ToString();
        }

        if (targetType == typeof(bool))
        {
            return valueElement.GetBoolean();
        }

        return Convert.ChangeType(valueElement.GetDouble(), targetType, CultureInfo.InvariantCulture);
    }

    private DateTime ReadTimestamp(JsonElement root)
    {
        if (!root.TryGetProperty("t", out var timestampElement)
            || !TryReadRemoteSeconds(timestampElement, out var remoteSeconds))
        {
            return DateTime.UtcNow;
        }

        lock (timestampLock)
        {
            if (remoteTimestampBaseUtc == null
                || firstRemoteSeconds == null
                || lastRemoteSeconds == null
                || remoteSeconds < lastRemoteSeconds.Value)
            {
                remoteTimestampBaseUtc = DateTime.UtcNow;
                firstRemoteSeconds = remoteSeconds;
                lastRemoteSeconds = remoteSeconds;
                return remoteTimestampBaseUtc.Value;
            }

            lastRemoteSeconds = remoteSeconds;
            return remoteTimestampBaseUtc.Value + TimeSpan.FromSeconds(remoteSeconds - firstRemoteSeconds.Value);
        }
    }

    private void ResetRemoteTimestampMapping()
    {
        lock (timestampLock)
        {
            remoteTimestampBaseUtc = null;
            firstRemoteSeconds = null;
            lastRemoteSeconds = null;
        }
    }

    private static bool TryReadRemoteSeconds(JsonElement timestampElement, out double remoteSeconds)
    {
        remoteSeconds = 0;
        if (timestampElement.ValueKind == JsonValueKind.Number)
        {
            return timestampElement.TryGetDouble(out remoteSeconds)
                   && double.IsFinite(remoteSeconds)
                   && remoteSeconds >= 0;
        }

        if (timestampElement.ValueKind == JsonValueKind.String)
        {
            return double.TryParse(
                       timestampElement.GetString(),
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       out remoteSeconds)
                   && double.IsFinite(remoteSeconds)
                   && remoteSeconds >= 0;
        }

        return false;
    }

    private static Dictionary<string, string> ParseParameters(string commParams)
    {
        return commParams
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(parameter => parameter.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(
                parts => parts[0],
                parts => parts[1].Trim('"'),
                StringComparer.OrdinalIgnoreCase);
    }
}
