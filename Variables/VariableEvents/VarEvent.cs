namespace Qenex.QSuite.Variables.VariableEvents;

public class VarEvent : IVarEvent
{
    public string Name { get; set; } = string.Empty;

    public string EventExtraParams { get; set; } = string.Empty;
}
