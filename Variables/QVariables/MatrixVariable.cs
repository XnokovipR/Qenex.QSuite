#nullable enable
using System.Buffers.Binary;
using System.Globalization;
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.Variables.ValueConversion;

namespace Qenex.QSuite.Variables.QVariables;

/// <summary>
/// Byte order of multi-byte elements within a MatrixVariable raw buffer.
/// </summary>
public enum MatrixEndianness { Little = 0, Big }

/// <summary>
/// Addresses one section of a MatrixVariable.
/// </summary>
public enum MatrixSectionKind { XAxis, YAxis, Data }

/// <summary>
/// Variable holding a byte array interpreted as a table with optional axes
/// (calibration value block / curve / map, ASAP2 VAL_BLK / CURVE / MAP).
/// The shape is given by which axes are present: none = value block,
/// X = curve, X + Y = map. The raw buffer layout is X axis values, then
/// Y axis values, then data values; data of a map is stored row-major
/// (index = y * XCount + x). Each section may override the element type and
/// carries its own presentation (conversion, print format, unit).
/// </summary>
public class MatrixVariable : VariableBase
{
    private byte[] rawData = [];

    #region Layout properties

    /// <summary>
    /// Element type used by sections whose DataType is Undefined.
    /// </summary>
    public ValuesGlobal.ValueDataType DefaultDataType { get; set; } = ValuesGlobal.ValueDataType.Byte;

    /// <summary>
    /// Byte order of multi-byte elements; applies to the whole raw buffer.
    /// </summary>
    public MatrixEndianness Endianness { get; set; } = MatrixEndianness.Little;

    /// <summary>
    /// Column axis (breakpoints); null when the matrix has no axes.
    /// </summary>
    public MatrixSection? XAxis { get; set; }

    /// <summary>
    /// Row axis (breakpoints); null for value block and curve shapes. Requires XAxis.
    /// </summary>
    public MatrixSection? YAxis { get; set; }

    /// <summary>
    /// Data block; always present. Its Count is only used when the matrix has no axes.
    /// </summary>
    public MatrixSection Data { get; set; } = new();

    #endregion

    #region Derived layout

    public int XCount => XAxis?.Count ?? 0;
    public int YCount => YAxis?.Count ?? 0;

    /// <summary>
    /// Number of data values: explicit Data.Count without axes, XCount for a curve,
    /// XCount * YCount for a map.
    /// </summary>
    public int DataCount => XAxis == null
        ? Data.Count
        : YAxis == null ? XAxis.Count : XAxis.Count * YAxis.Count;

    /// <summary>
    /// Total size of the raw buffer in bytes, derived from section counts and element types.
    /// </summary>
    public int Size =>
        XCount * GetElementSize(MatrixSectionKind.XAxis)
        + YCount * GetElementSize(MatrixSectionKind.YAxis)
        + DataCount * GetElementSize(MatrixSectionKind.Data);

    public MatrixSection? GetSection(MatrixSectionKind kind) => kind switch
    {
        MatrixSectionKind.XAxis => XAxis,
        MatrixSectionKind.YAxis => YAxis,
        _ => Data
    };

    public int GetSectionCount(MatrixSectionKind kind) => kind switch
    {
        MatrixSectionKind.XAxis => XCount,
        MatrixSectionKind.YAxis => YCount,
        _ => DataCount
    };

    /// <summary>
    /// Effective element type of the section (section override or DefaultDataType).
    /// </summary>
    public ValuesGlobal.ValueDataType GetSectionDataType(MatrixSectionKind kind)
    {
        var sectionType = GetSection(kind)?.DataType ?? ValuesGlobal.ValueDataType.Undefined;
        return sectionType != ValuesGlobal.ValueDataType.Undefined ? sectionType : DefaultDataType;
    }

    /// <summary>
    /// Byte offset of the section within the raw buffer.
    /// </summary>
    public int GetSectionOffset(MatrixSectionKind kind) => kind switch
    {
        MatrixSectionKind.XAxis => 0,
        MatrixSectionKind.YAxis => XCount * GetElementSize(MatrixSectionKind.XAxis),
        _ => XCount * GetElementSize(MatrixSectionKind.XAxis)
             + YCount * GetElementSize(MatrixSectionKind.YAxis)
    };

    public int GetElementSize(MatrixSectionKind kind) => ElementSize(GetSectionDataType(kind));

    /// <summary>
    /// Returns null when the layout is valid, otherwise a description of the first problem.
    /// </summary>
    public string? ValidateLayout()
    {
        if (YAxis != null && XAxis == null)
        {
            return "Y axis requires X axis.";
        }

        foreach (var kind in new[] { MatrixSectionKind.XAxis, MatrixSectionKind.YAxis, MatrixSectionKind.Data })
        {
            if (GetSection(kind) == null)
            {
                continue;
            }

            var dataType = GetSectionDataType(kind);
            if (dataType is ValuesGlobal.ValueDataType.Undefined or ValuesGlobal.ValueDataType.String)
            {
                return $"Section {kind} has unsupported element type {dataType}.";
            }

            if (GetSectionCount(kind) < 1)
            {
                return $"Section {kind} must have at least one value.";
            }
        }

        return null;
    }

    #endregion

    #region Raw buffer

    /// <summary>
    /// Raw buffer of the whole matrix. Reallocated (zeroed) when the layout size changed.
    /// </summary>
    public byte[] RawData
    {
        get
        {
            if (rawData.Length != Size)
            {
                rawData = new byte[Size];
            }
            return rawData;
        }
    }

    public override object GetValue()
    {
        return RawData;
    }

    /// <summary>
    /// Sets the whole raw buffer; the value must be a byte array of exactly Size bytes.
    /// </summary>
    public override void SetValue(object value)
    {
        if (value is not byte[] bytes)
        {
            throw new InvalidCastException($"Cannot cast value of type {value.GetType()} to byte[].");
        }

        if (bytes.Length != Size)
        {
            throw new ArgumentException($"Expected {Size} bytes for variable {Name}, got {bytes.Length}.");
        }

        rawData = bytes;
    }

    #endregion

    #region Element access

    /// <summary>
    /// Raw value of one element, decoded from the buffer by section type and endianness.
    /// </summary>
    public double GetRawValue(MatrixSectionKind kind, int index)
    {
        return ReadElement(GetSectionDataType(kind), ElementSpan(kind, index));
    }

    /// <summary>
    /// Engineering value = Conversion(raw). Linear: raw*Multiplier+Offset; otherwise raw.
    /// </summary>
    public double GetEngValue(MatrixSectionKind kind, int index)
    {
        var raw = GetRawValue(kind, index);
        return GetSection(kind)?.Presentation?.Conversion is LinearValConversion linear
            ? linear.Apply(raw)
            : raw;
    }

    /// <summary>
    /// Writes an engineering value into one element: eng -> raw via the inverse
    /// conversion (Linear.Invert; otherwise 1:1), rounded and range-checked for
    /// integer types. Returns false on overflow/NaN.
    /// </summary>
    public bool TrySetEngValue(MatrixSectionKind kind, int index, double engValue)
    {
        var raw = GetSection(kind)?.Presentation?.Conversion is LinearValConversion linear
            ? linear.Invert(engValue)
            : engValue;

        try
        {
            WriteElement(GetSectionDataType(kind), ElementSpan(kind, index), raw);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Display text of one element: enum conversion -> state name; otherwise the
    /// engineering value formatted by the section PrintFormat.
    /// </summary>
    public string GetPresentationText(MatrixSectionKind kind, int index)
    {
        var presentation = GetSection(kind)?.Presentation;
        var raw = GetRawValue(kind, index);

        if (presentation == null)
        {
            return raw.ToString(CultureInfo.InvariantCulture);
        }

        if (presentation.Conversion is EnumValConversion enumConversion)
        {
            return enumConversion.Map(raw) ?? raw.ToString(CultureInfo.InvariantCulture);
        }

        var eng = GetEngValue(kind, index);
        return string.IsNullOrEmpty(presentation.PrintFormat)
            ? eng.ToString(CultureInfo.InvariantCulture)
            : string.Format(CultureInfo.InvariantCulture, presentation.PrintFormat, eng);
    }

    private Span<byte> ElementSpan(MatrixSectionKind kind, int index)
    {
        if (GetSection(kind) == null)
        {
            throw new InvalidOperationException($"Variable {Name} has no {kind} section.");
        }

        var count = GetSectionCount(kind);
        if (index < 0 || index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} out of range 0..{count - 1} of section {kind}.");
        }

        var elementSize = GetElementSize(kind);
        return RawData.AsSpan(GetSectionOffset(kind) + index * elementSize, elementSize);
    }

    #endregion

    #region Element encoding

    private static int ElementSize(ValuesGlobal.ValueDataType dataType) => dataType switch
    {
        ValuesGlobal.ValueDataType.Byte or ValuesGlobal.ValueDataType.SByte => 1,
        ValuesGlobal.ValueDataType.UShort or ValuesGlobal.ValueDataType.Short => 2,
        ValuesGlobal.ValueDataType.UInt or ValuesGlobal.ValueDataType.Int or ValuesGlobal.ValueDataType.Float => 4,
        ValuesGlobal.ValueDataType.ULong or ValuesGlobal.ValueDataType.Long or ValuesGlobal.ValueDataType.Double => 8,
        _ => throw new InvalidOperationException($"Unsupported element type {dataType}.")
    };

    private double ReadElement(ValuesGlobal.ValueDataType dataType, ReadOnlySpan<byte> bytes)
    {
        var little = Endianness == MatrixEndianness.Little;
        return dataType switch
        {
            ValuesGlobal.ValueDataType.Byte => bytes[0],
            ValuesGlobal.ValueDataType.SByte => (sbyte)bytes[0],
            ValuesGlobal.ValueDataType.UShort => little ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) : BinaryPrimitives.ReadUInt16BigEndian(bytes),
            ValuesGlobal.ValueDataType.Short => little ? BinaryPrimitives.ReadInt16LittleEndian(bytes) : BinaryPrimitives.ReadInt16BigEndian(bytes),
            ValuesGlobal.ValueDataType.UInt => little ? BinaryPrimitives.ReadUInt32LittleEndian(bytes) : BinaryPrimitives.ReadUInt32BigEndian(bytes),
            ValuesGlobal.ValueDataType.Int => little ? BinaryPrimitives.ReadInt32LittleEndian(bytes) : BinaryPrimitives.ReadInt32BigEndian(bytes),
            ValuesGlobal.ValueDataType.ULong => little ? BinaryPrimitives.ReadUInt64LittleEndian(bytes) : BinaryPrimitives.ReadUInt64BigEndian(bytes),
            ValuesGlobal.ValueDataType.Long => little ? BinaryPrimitives.ReadInt64LittleEndian(bytes) : BinaryPrimitives.ReadInt64BigEndian(bytes),
            ValuesGlobal.ValueDataType.Float => little ? BinaryPrimitives.ReadSingleLittleEndian(bytes) : BinaryPrimitives.ReadSingleBigEndian(bytes),
            ValuesGlobal.ValueDataType.Double => little ? BinaryPrimitives.ReadDoubleLittleEndian(bytes) : BinaryPrimitives.ReadDoubleBigEndian(bytes),
            _ => throw new InvalidOperationException($"Unsupported element type {dataType}.")
        };
    }

    private void WriteElement(ValuesGlobal.ValueDataType dataType, Span<byte> bytes, double raw)
    {
        var little = Endianness == MatrixEndianness.Little;
        switch (dataType)
        {
            case ValuesGlobal.ValueDataType.Byte:
                bytes[0] = checked((byte)Math.Round(raw));
                break;
            case ValuesGlobal.ValueDataType.SByte:
                bytes[0] = unchecked((byte)checked((sbyte)Math.Round(raw)));
                break;
            case ValuesGlobal.ValueDataType.UShort:
                var us = checked((ushort)Math.Round(raw));
                if (little) BinaryPrimitives.WriteUInt16LittleEndian(bytes, us); else BinaryPrimitives.WriteUInt16BigEndian(bytes, us);
                break;
            case ValuesGlobal.ValueDataType.Short:
                var s = checked((short)Math.Round(raw));
                if (little) BinaryPrimitives.WriteInt16LittleEndian(bytes, s); else BinaryPrimitives.WriteInt16BigEndian(bytes, s);
                break;
            case ValuesGlobal.ValueDataType.UInt:
                var ui = checked((uint)Math.Round(raw));
                if (little) BinaryPrimitives.WriteUInt32LittleEndian(bytes, ui); else BinaryPrimitives.WriteUInt32BigEndian(bytes, ui);
                break;
            case ValuesGlobal.ValueDataType.Int:
                var i = checked((int)Math.Round(raw));
                if (little) BinaryPrimitives.WriteInt32LittleEndian(bytes, i); else BinaryPrimitives.WriteInt32BigEndian(bytes, i);
                break;
            case ValuesGlobal.ValueDataType.ULong:
                var ul = checked((ulong)Math.Round(raw));
                if (little) BinaryPrimitives.WriteUInt64LittleEndian(bytes, ul); else BinaryPrimitives.WriteUInt64BigEndian(bytes, ul);
                break;
            case ValuesGlobal.ValueDataType.Long:
                var l = checked((long)Math.Round(raw));
                if (little) BinaryPrimitives.WriteInt64LittleEndian(bytes, l); else BinaryPrimitives.WriteInt64BigEndian(bytes, l);
                break;
            case ValuesGlobal.ValueDataType.Float:
                var f = (float)raw;
                if (little) BinaryPrimitives.WriteSingleLittleEndian(bytes, f); else BinaryPrimitives.WriteSingleBigEndian(bytes, f);
                break;
            case ValuesGlobal.ValueDataType.Double:
                if (little) BinaryPrimitives.WriteDoubleLittleEndian(bytes, raw); else BinaryPrimitives.WriteDoubleBigEndian(bytes, raw);
                break;
            default:
                throw new InvalidOperationException($"Unsupported element type {dataType}.");
        }
    }

    #endregion
}
