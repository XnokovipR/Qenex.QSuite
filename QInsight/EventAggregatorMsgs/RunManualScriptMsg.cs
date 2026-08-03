using Qenex.QInsight.ViewModels.ModelWrappers;

namespace Qenex.QInsight.EventAggregatorMsgs;

/// <summary>Request from a script editor to run its Manual-mode script now.</summary>
public class RunManualScriptMsg
{
    public required ScriptWrapper Script { get; init; }
}
