using System.Globalization;
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.Variables.ValueConversion;

namespace Qenex.QSuite.Variables.QVariables;

public class ScalarVariable : VariableBase
{
    public int Size { get; set; }
    public IValuesBase Values { get; set; }

    /// <summary>
    /// Bit-field extraction applied between the raw value and the conversion (A2L BIT_MASK style):
    /// field = (raw >> BitShift) &amp; BitMask. Positive shift = right, negative = left, 0 = none.
    /// BitMask 0 = no masking. The raw value (Values) always holds the whole word as communicated;
    /// several variables on the same address with different shift/mask expose the word's fields.
    /// Only integer value types support bit fields; for Float/Double/String it is ignored.
    /// </summary>
    public int BitShift { get; set; }

    public ulong BitMask { get; set; }

    public bool HasBitField => (BitShift != 0 || BitMask != 0) && IsIntegerType(Values?.ValueType);

    public override object GetValue()
    {
        return Values.GetValue();
    }

    public override void SetValue(object value)
    {
        Values.SetValue(value);
    }

    /// <summary>
    /// Raw value entering the conversion: the bit field when one is configured, otherwise the whole raw value.
    /// </summary>
    public double GetConversionInput()
    {
        var rawObject = Values.GetValue();
        return HasBitField ? ExtractBitField(ToBits(rawObject)) : ToDouble(rawObject);
    }

    /// <summary>
    /// Engineering hodnota = Conversion(raw). Linear: raw*Multiplier+Offset; jinak (zadna / enum) = raw.
    /// Pocita se na vyzadani z aktualni raw hodnoty a ValPresentation.Conversion.
    /// </summary>
    public double GetEngValue()
    {
        var raw = GetConversionInput();
        return Values.ValPresentation?.Conversion is LinearValConversion linear
            ? linear.Apply(raw)
            : raw;
    }

    /// <summary>
    /// Zapis inzenyrske hodnoty: eng -> raw pres inverzni konverzi (Linear.Invert; jinak 1:1),
    /// prevod na typ Values (celociselne typy se zaokrouhluji, checked). Vraci false pri
    /// preteceni/NaN nebo nepodporovanem typu (String/Undefined).
    /// With a bit field the inverted value is placed back into the field position by a
    /// read-modify-write of the current raw word (other bits keep their last read value).
    /// </summary>
    public bool TrySetEngValue(double engValue)
    {
        var raw = Values.ValPresentation?.Conversion is LinearValConversion linear
            ? linear.Invert(engValue)
            : engValue;

        try
        {
            if (HasBitField)
            {
                var field = checked((ulong)Math.Round(raw));
                if ((field & ~FieldMask) != 0)
                {
                    return false; // value does not fit into the bit field
                }

                Values.SetValue(FromBits(InsertBitField(ToBits(Values.GetValue()), field)));
                return true;
            }

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
            return HasBitField
                ? GetConversionInput().ToString(CultureInfo.InvariantCulture)
                : rawObject?.ToString() ?? string.Empty;
        }

        if (presentation.Conversion is EnumValConversion enumConversion)
        {
            var conversionInput = GetConversionInput();
            return enumConversion.Map(conversionInput)
                   ?? (HasBitField ? conversionInput.ToString(CultureInfo.InvariantCulture) : rawObject?.ToString())
                   ?? string.Empty;
        }

        var eng = GetEngValue();
        return string.IsNullOrEmpty(presentation.PrintFormat)
            ? eng.ToString(CultureInfo.InvariantCulture)
            : string.Format(CultureInfo.InvariantCulture, presentation.PrintFormat, eng);
    }

    #region Bit field

    /// <summary>Parses ">>n" (right), "&lt;&lt;n" (left) or a plain integer (positive = right); empty = 0.</summary>
    public static bool TryParseBitShift(string? text, out int shift)
    {
        shift = 0;
        var trimmed = (text ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return true;
        }

        var sign = 1;
        if (trimmed.StartsWith(">>", StringComparison.Ordinal))
        {
            trimmed = trimmed[2..].Trim();
        }
        else if (trimmed.StartsWith("<<", StringComparison.Ordinal))
        {
            sign = -1;
            trimmed = trimmed[2..].Trim();
        }

        if (!int.TryParse(trimmed, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var count)
            || Math.Abs(count) > 63)
        {
            return false;
        }

        shift = sign * count;
        return true;
    }

    /// <summary>Formats the shift as ">>n" / "&lt;&lt;n"; 0 = empty string.</summary>
    public static string FormatBitShift(int shift)
    {
        return shift switch
        {
            0 => string.Empty,
            > 0 => $">>{shift}",
            _ => $"<<{-shift}"
        };
    }

    /// <summary>Parses a mask given as hex ("0x0F") or decimal ("15"); empty = 0 (no mask).</summary>
    public static bool TryParseBitMask(string? text, out ulong mask)
    {
        mask = 0;
        var trimmed = (text ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return true;
        }

        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return ulong.TryParse(trimmed[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out mask);
        }

        return ulong.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out mask);
    }

    /// <summary>Formats the mask as "0x.." (at least two hex digits); 0 = empty string.</summary>
    public static string FormatBitMask(ulong mask)
    {
        return mask == 0 ? string.Empty : $"0x{mask:X2}";
    }

    private static bool IsIntegerType(ValuesGlobal.ValueDataType? valueType)
    {
        return valueType is ValuesGlobal.ValueDataType.Byte or ValuesGlobal.ValueDataType.SByte
            or ValuesGlobal.ValueDataType.UShort or ValuesGlobal.ValueDataType.Short
            or ValuesGlobal.ValueDataType.UInt or ValuesGlobal.ValueDataType.Int
            or ValuesGlobal.ValueDataType.ULong or ValuesGlobal.ValueDataType.Long;
    }

    // All-ones mask for the bit width of the value type (raw word width).
    private ulong WidthMask => Values.ValueType switch
    {
        ValuesGlobal.ValueDataType.Byte or ValuesGlobal.ValueDataType.SByte => 0xFF,
        ValuesGlobal.ValueDataType.UShort or ValuesGlobal.ValueDataType.Short => 0xFFFF,
        ValuesGlobal.ValueDataType.UInt or ValuesGlobal.ValueDataType.Int => 0xFFFFFFFF,
        _ => ulong.MaxValue
    };

    // Mask of the field itself (before shifting into place): explicit BitMask or the whole word width.
    private ulong FieldMask => BitMask != 0 ? BitMask : WidthMask;

    private ulong ExtractBitField(ulong bits)
    {
        var field = BitShift >= 0 ? bits >> BitShift : bits << -BitShift;
        field &= WidthMask;
        return BitMask != 0 ? field & BitMask : field;
    }

    // Places the field back into the word: field bits are shifted the opposite way, everything
    // outside the field position keeps its current value.
    private ulong InsertBitField(ulong word, ulong field)
    {
        var placedField = BitShift >= 0 ? (field & FieldMask) << BitShift : (field & FieldMask) >> -BitShift;
        var placeMask = BitShift >= 0 ? FieldMask << BitShift : FieldMask >> -BitShift;
        placeMask &= WidthMask;
        return (word & ~placeMask) | (placedField & placeMask);
    }

    // Raw word as unsigned bits of the type's width (signed types: two's complement, no sign extension).
    private ulong ToBits(object? value)
    {
        return value switch
        {
            byte b => b,
            sbyte sb => unchecked((byte)sb),
            ushort us => us,
            short s => unchecked((ushort)s),
            uint ui => ui,
            int i => unchecked((uint)i),
            ulong ul => ul,
            long l => unchecked((ulong)l),
            _ => 0
        };
    }

    private object FromBits(ulong bits)
    {
        return Values.ValueType switch
        {
            ValuesGlobal.ValueDataType.Byte => (object)unchecked((byte)bits),
            ValuesGlobal.ValueDataType.SByte => (object)unchecked((sbyte)(byte)bits),
            ValuesGlobal.ValueDataType.UShort => (object)unchecked((ushort)bits),
            ValuesGlobal.ValueDataType.Short => (object)unchecked((short)(ushort)bits),
            ValuesGlobal.ValueDataType.UInt => (object)unchecked((uint)bits),
            ValuesGlobal.ValueDataType.Int => (object)unchecked((int)(uint)bits),
            ValuesGlobal.ValueDataType.ULong => (object)bits,
            ValuesGlobal.ValueDataType.Long => (object)unchecked((long)bits),
            _ => throw new InvalidCastException($"Unsupported value type {Values.ValueType} for bit field.")
        };
    }

    #endregion

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
