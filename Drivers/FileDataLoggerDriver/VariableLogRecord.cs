using MessagePack;

namespace Qenex.QSuite.Drivers.FileDataLoggerDriver;

[MessagePackObject]
public sealed class VariableLogRecord
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
}
