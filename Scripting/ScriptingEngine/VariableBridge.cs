using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Scripting.ScriptingEngine;

public class VariableBridge(IVariableBase variable)
{
    public string Name => variable.Name;
    public int Id => variable.Id;
    public object Value
    {
        get => variable.GetValue();
        set => variable.SetValue(value);
    }
    
    public override string ToString() => variable.GetValue().ToString() ?? "null";
}
