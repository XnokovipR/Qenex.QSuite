using Qenex.QSuite.Variables.QVariables;
using Python.Runtime;
using System.Globalization;

namespace Qenex.QSuite.Scripting.ScriptingEngine;

public class VariableBridge(IVariableBase variable)
{
    public string Name => variable.Name;
    public int Id => variable.Id;
    public object Value
    {
        get => variable.GetValue();
        set => variable.SetValue(ConvertValueForVariable(value));
    }

    public override string ToString() => variable.GetValue().ToString() ?? "null";

    private object ConvertValueForVariable(object value)
    {
        var currentValue = variable.GetValue();
        if (currentValue == null)
        {
            return value;
        }

        var targetType = currentValue.GetType();
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (value is PyObject pyObject)
        {
            using (Py.GIL())
            {
                return pyObject.AsManagedObject(targetType)
                    ?? throw new InvalidCastException($"Cannot convert Python value of type {value.GetType()} to {targetType}.");
            }
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }
}
