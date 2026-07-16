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
    /// Zapis inzenyrske hodnoty: eng -> raw pres inverzni konverzi (Linear.Invert; jinak 1:1),
    /// prevod na typ Values (celociselne typy se zaokrouhluji, checked). Vraci false pri
    /// preteceni/NaN nebo nepodporovanem typu (String/Undefined).
    /// </summary>
    public bool TrySetEngValue(double engValue)
    {
        var raw = Values.ValPresentation?.Conversion is LinearValConversion linear
            ? linear.Invert(engValue)
            : engValue;

        try
        {
            // Kazde rameno explicitne boxovat jako cilovy typ - bez (object) by switch
            // expression pouzil spolecny typ double a Values<T>.SetValue by odmitl cast
            object converted = Values.ValueType switch
            {
                ValuesGlobal.ValueDataType.Byte => (object)checked((byte)Math.Round(raw)),
                ValuesGlobal.ValueDataType.SByte => (object)checked((sbyte)Math.Round(raw)),
                ValuesGlobal.ValueDataType.UShort => (object)checked((ushort)Math.Round(raw)),
                ValuesGlobal.ValueDataType.Short => (object)checked((short)Math.Round(raw)),
                ValuesGlobal.ValueDataType.UInt => (object)checked((uint)Math.Round(raw)),
                ValuesGlobal.ValueDataType.Int => (object)checked((int)Math.Round(raw)),
                ValuesGlobal.ValueDataType.ULong => (object)checked((ulong)Math.Round(raw)),
                ValuesGlobal.ValueDataType.Long => (object)checked((long)Math.Round(raw)),
                ValuesGlobal.ValueDataType.Float => (object)(float)raw,
                ValuesGlobal.ValueDataType.Double => raw,
                _ => throw new InvalidCastException($"Unsupported value type {Values.ValueType} for engineering write.")
            };

            Values.SetValue(converted);
            return true;
        }
        catch
        {
            return false;
        }
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
