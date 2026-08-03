using Qenex.QInsight.ViewModels.ModelWrappers;

namespace Qenex.QInsight.EventAggregatorMsgs;

/// <summary>Request from a script editor to stop its running Manual-mode script.</summary>
public class StopManualScriptMsg
{
    public required ScriptWrapper Script { get; init; }
}
