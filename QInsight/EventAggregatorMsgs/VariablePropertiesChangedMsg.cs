using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QInsight.EventAggregatorMsgs;

public class VariablePropertiesChangedMsg
{
    public IVariableBase Variable { get; set; } = null!;
}
