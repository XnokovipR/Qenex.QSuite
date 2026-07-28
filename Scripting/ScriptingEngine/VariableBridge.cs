using Qenex.QSuite.Variables.QVariables;
using Python.Runtime;
using System.Globalization;

namespace Qenex.QSuite.Scripting.ScriptingEngine;

public class VariableBridge(IVariableBase variable, Func<Action<IVariableBase>?>? variableWrittenCallbackProvider = null)
{
    public string Name => variable.Name;
    public int Id => variable.Id;

    /// <summary>
    /// Syrova hodnota z dratu (raw) - cteni i zapis. Drive se jmenovala Value;
    /// prejmenovano kvuli jednoznacnosti vuci EngValue (viz value model Raw/Eng/Presentation).
    /// </summary>
    public object RawValue
    {
        get => variable.GetValue();
        set
        {
            variable.SetValue(ConvertValueForVariable(value));
            NotifyVariableWritten();
        }
    }

    /// <summary>
    /// Engineering value = Conversion(raw). For a scalar variable (Linear: raw*Mult+Offset,
    /// otherwise = raw); other variable types read raw as double. Writing goes through the
    /// inverse conversion (eng -> raw, see ScalarVariable.TrySetEngValue) and throws into the
    /// script on overflow/NaN or an unsupported variable type, so the author sees the problem
    /// instead of a silently unchanged value.
    /// </summary>
    public double EngValue
    {
        get => variable is ScalarVariable scalar
            ? scalar.GetEngValue()
            : ToDouble(variable.GetValue());
        set
        {
            if (variable is not ScalarVariable scalarVariable)
            {
                throw new InvalidOperationException(
                    $"Variable '{variable.Name}' does not support engineering-value writes.");
            }

            if (!scalarVariable.TrySetEngValue(value))
            {
                throw new InvalidOperationException(
                    $"Engineering value {value} cannot be converted to the raw type of variable '{variable.Name}' (overflow, NaN or unsupported type).");
            }

            NotifyVariableWritten();
        }
    }

    // A successful write is reported to the host so protocols publishing script-computed
    // variables can enqueue the new sample; runs on the Python thread, so it must not block.
    private void NotifyVariableWritten()
    {
        variableWrittenCallbackProvider?.Invoke()?.Invoke(variable);
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

    private static double ToDouble(object? value)
    {
        if (value == null) return 0;
        try
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return 0;
        }
    }
}
