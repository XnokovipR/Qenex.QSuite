using MessagePack;
using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.Drivers.FileDataReplayDriver;

[MessagePackObject]
public sealed class SerializedVariableLogRecord
{
    [Key(0)] public int Version { get; set; } = 1;
    [Key(1)] public string RecordKind { get; set; } = "VariableValue";
    [Key(2)] public long TimestampUtcTicks { get; set; }
    [Key(3)] public int VariableId { get; set; }
    [Key(4)] public string VariableNamespace { get; set; } = string.Empty;
    [Key(5)] public string VariableName { get; set; } = string.Empty;
    [Key(6)] public string VariableLabel { get; set; } = string.Empty;
    [Key(7)] public string ValueType { get; set; } = string.Empty;
    [Key(8)] public string Value { get; set; } = string.Empty;

    public DataLogRecord ToDataLogRecord()
    {
        return new DataLogRecord
        {
            Version = Version,
            RecordKind = RecordKind,
            TimestampUtcTicks = TimestampUtcTicks,
            VariableId = VariableId,
            VariableNamespace = VariableNamespace,
            VariableName = VariableName,
            VariableLabel = VariableLabel,
            ValueType = ValueType,
            Value = Value
        };
    }
}
