using System.Globalization;
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.Variables.ValueConversion;

namespace Qenex.QSuite.Variables.QVariables;

public class ScalarVariable : VariableBase
{
    public int Size { get; set; }
    public IValuesBase Values { get; set; }

    public override object GetValue()
    {
        return Values.GetValue();
    }

    public override void SetValue(object value)
    {
        Values.SetValue(value);
    }

    /// <summary>
    /// Engineering hodnota = Conversion(raw). Linear: raw*Multiplier+Offset; jinak (zadna / enum) = raw.
    /// Pocita se na vyzadani z aktualni raw hodnoty a ValPresentation.Conversion.
    /// </summary>
    public double GetEngValue()
    {
        var raw = ToDouble(Values.GetValue());
        return Values.ValPresentation?.Conversion is LinearValConversion linear
            ? linear.Apply(raw)
            : raw;
    }

    /// <summary>
    /// Zobrazovaci text: enum -> nazev stavu; jinak naformatovana EngValue dle PrintFormat.
    /// Jednotku si controly pripojuji samy (Unit), proto tu neni.
    /// </summary>
    public string GetPresentationText()
    {
        var presentation = Values.ValPresentation;
        var rawObject = Values.GetValue();

        if (presentation == null)
        {
            return rawObject?.ToString() ?? string.Empty;
        }

        if (presentation.Conversion is EnumValConversion enumConversion)
        {
            return enumConversion.Map(ToDouble(rawObject)) ?? rawObject?.ToString() ?? string.Empty;
        }

        var eng = GetEngValue();
        return string.IsNullOrEmpty(presentation.PrintFormat)
            ? eng.ToString(CultureInfo.InvariantCulture)
            : string.Format(CultureInfo.InvariantCulture, presentation.PrintFormat, eng);
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
