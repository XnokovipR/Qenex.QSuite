using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QInsight.EventAggregatorMsgs;

/// <summary>
/// Collector message published before a variable is deleted. Every open workspace
/// fills <see cref="Usages"/> with the controls that are bound to <see cref="Variable"/>.
/// Mirrors the <see cref="VariablePropertiesChangedMsg"/> broadcast, but collects a result.
/// </summary>
public class VariableUsageQuery
{
    public IVariableBase Variable { get; set; } = null!;

    public List<VariableUsage> Usages { get; } = [];
}

public class VariableUsage
{
    public string WorkspaceName { get; set; } = string.Empty;

    public string ControlLabel { get; set; } = string.Empty;
}
