using Qenex.QSuite.Variables.QVariables;
using Python.Runtime;
using System.Globalization;

namespace Qenex.QSuite.Scripting.ScriptingEngine;

public class VariableBridge(IVariableBase variable)
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
        set => variable.SetValue(ConvertValueForVariable(value));
    }

    /// <summary>
    /// Engineering hodnota = Conversion(raw). Pro skalarni promennou (Linear: raw*Mult+Offset,
    /// jinak = raw); pro ostatni typy raw jako double. Zatim read-only (zapis pres inverzni
    /// konverzi je soucasti budouciho zapisoveho smeru).
    /// </summary>
    public double EngValue => variable is ScalarVariable scalar
        ? scalar.GetEngValue()
        : ToDouble(variable.GetValue());

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
