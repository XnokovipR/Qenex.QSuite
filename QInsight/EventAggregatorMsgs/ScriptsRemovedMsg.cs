using Qenex.QSuite.Scripting.Script;

namespace Qenex.QInsight.EventAggregatorMsgs;

public class ScriptsRemovedMsg
{
    public IReadOnlyCollection<IScriptBase> Scripts { get; init; } = [];
}
