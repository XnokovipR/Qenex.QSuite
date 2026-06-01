using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.DataLogReplayProtocol;

public class DataLogReplayProtocol : ProtocolBase<DataLogRecord>
{
    private volatile bool exitRequested;
    private readonly EventWaitHandle waitHandle;
    private readonly ConcurrentQueue<DataLogRecord> receivedDataQueue;

    public DataLogReplayProtocol()
    {
        waitHandle = new AutoResetEvent(false);
        receivedDataQueue = new ConcurrentQueue<DataLogRecord>();

        Specification = new SpecificationBase
        {
            Name = "DataLogReplayProtocol",
            Label = "Data Log Replay Protocol",
            Description = "Replays logged variable values into protocol variables.",
            CreatedOn = new DateTime(2026, 5, 25),
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
        return new DataLogReplayProtocolVariable
        {
            Variable = variable,
            IsCommunicated = isCommunicated,
            ProtocolVariableSpecification = DataLogReplayProtocolVariableSpecification.Create(commParams)
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

    public override Task AddReceivedDataToQueueAsync(IEnumerable<DataLogRecord> data, CancellationToken ct = default)
    {
        foreach (var record in data)
        {
            receivedDataQueue.Enqueue(record);
        }

        waitHandle.Set();
        return Task.CompletedTask;
    }

    protected override void ProcessReceivedData(IEnumerable<DataLogRecord> data)
    {
        foreach (var record in data)
        {
            var protocolVariable = ApplyRecord(record);
            protocolVariable?.NotifyValueChanged();
        }
    }

    protected override async Task ProcessReceivedDataAsync(IEnumerable<DataLogRecord> data, CancellationToken ct = default)
    {
        var notifyTasks = new List<Task>();
        foreach (var record in data)
        {
            ct.ThrowIfCancellationRequested();
            var protocolVariable = ApplyRecord(record);
            if (protocolVariable != null)
            {
                notifyTasks.Add(protocolVariable.NotifyValueChangedAsync());
            }
        }

        await Task.WhenAll(notifyTasks);
    }

    protected override IEnumerable<DataLogRecord> Encode(IEnumerable<IProtocolVariable> protocolVariables)
    {
        throw new NotSupportedException("Data log replay protocol does not encode outgoing data.");
    }

    protected override IEnumerable<IProtocolVariable> Decode(IEnumerable<DataLogRecord> data)
    {
        return data
            .Select(ApplyRecord)
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
                    receivedDataQueue.TryDequeue(out var record);
                    await ProcessReceivedDataAsync(new List<DataLogRecord> { record! }, ct);
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

    private IProtocolVariable? ApplyRecord(DataLogRecord record)
    {
        var protocolVariable = FindProtocolVariable(record);
        if (protocolVariable == null)
        {
            return null;
        }

        var variable = protocolVariable.Variable;
        variable.Timestamp = record.TimestampUtcTicks > 0
            ? new DateTime(record.TimestampUtcTicks, DateTimeKind.Utc)
            : DateTime.UtcNow;
        variable.SetValue(ConvertValue(record, variable));

        return protocolVariable;
    }

    private IProtocolVariable? FindProtocolVariable(DataLogRecord record)
    {
        return Variables.FirstOrDefault(variable => variable.Variable.Id == record.VariableId)
               ?? Variables.FirstOrDefault(variable =>
                   string.Equals(variable.Variable.Namespace, record.VariableNamespace, StringComparison.Ordinal)
                   && string.Equals(variable.Variable.Name, record.VariableName, StringComparison.Ordinal));
    }

    private static object ConvertValue(DataLogRecord record, IVariableBase variable)
    {
        var targetType = GetTargetType(record, variable);
        if (targetType == typeof(string))
        {
            return record.Value;
        }

        if (string.IsNullOrEmpty(record.Value))
        {
            return Activator.CreateInstance(targetType) ?? record.Value;
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, record.Value, ignoreCase: true);
        }

        return Convert.ChangeType(record.Value, targetType, CultureInfo.InvariantCulture);
    }

    private static Type GetTargetType(DataLogRecord record, IVariableBase variable)
    {
        if (variable is StringVariable)
        {
            return typeof(string);
        }

        var currentValue = variable.GetValue();
        if (currentValue != null)
        {
            return currentValue.GetType();
        }

        return Type.GetType(record.ValueType, throwOnError: false) ?? typeof(string);
    }
}
