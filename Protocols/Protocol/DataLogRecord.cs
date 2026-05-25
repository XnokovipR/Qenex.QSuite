namespace Qenex.QSuite.Protocols.Protocol;

public sealed class DataLogRecord
{
    public int Version { get; set; } = 1;
    public string RecordKind { get; set; } = "VariableValue";
    public long TimestampUtcTicks { get; set; }
    public int VariableId { get; set; }
    public string VariableNamespace { get; set; } = string.Empty;
    public string VariableName { get; set; } = string.Empty;
    public string VariableLabel { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
