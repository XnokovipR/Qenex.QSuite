using System.Buffers.Binary;
using ValueDataType = Qenex.QSuite.Variables.QVariables.Values.ValuesGlobal.ValueDataType;

namespace Qenex.QSuite.Protocols.Modbus;

/// <summary>
/// Converts between boxed scalar values and Modbus 16-bit registers. Bytes inside one register are
/// big-endian per the spec; the order of registers for multi-register values is configurable
/// (<see cref="ModbusWordOrder"/>) because devices differ. Bit-table (coil/discrete) variables are
/// converted through <see cref="ToBit"/>/<see cref="FromBit"/> — any non-zero value is ON.
/// </summary>
public static class ModbusRegisterCodec
{
    /// <summary>Number of 16-bit registers a value type occupies; 0 for unsupported types.</summary>
    public static int RegisterCount(ValueDataType type) => type switch
    {
        ValueDataType.Byte or ValueDataType.SByte or ValueDataType.UShort or ValueDataType.Short => 1,
        ValueDataType.UInt or ValueDataType.Int or ValueDataType.Float => 2,
        ValueDataType.ULong or ValueDataType.Long or ValueDataType.Double => 4,
        _ => 0
    };

    /// <summary>Encodes a boxed value (whose CLR type matches <paramref name="type"/>) into registers.</summary>
    public static ushort[] EncodeValue(object value, ValueDataType type, ModbusWordOrder wordOrder)
    {
        var registerCount = RegisterCount(type);
        if (registerCount == 0)
        {
            throw new ModbusProtocolException($"Value type '{type}' cannot be mapped to Modbus registers.");
        }

        Span<byte> bytes = stackalloc byte[8];
        try
        {
            switch (type)
            {
                case ValueDataType.Byte: BinaryPrimitives.WriteUInt16BigEndian(bytes, (byte)value); break;
                case ValueDataType.SByte: BinaryPrimitives.WriteInt16BigEndian(bytes, (sbyte)value); break;
                case ValueDataType.UShort: BinaryPrimitives.WriteUInt16BigEndian(bytes, (ushort)value); break;
                case ValueDataType.Short: BinaryPrimitives.WriteInt16BigEndian(bytes, (short)value); break;
                case ValueDataType.UInt: BinaryPrimitives.WriteUInt32BigEndian(bytes, (uint)value); break;
                case ValueDataType.Int: BinaryPrimitives.WriteInt32BigEndian(bytes, (int)value); break;
                case ValueDataType.Float: BinaryPrimitives.WriteSingleBigEndian(bytes, (float)value); break;
                case ValueDataType.ULong: BinaryPrimitives.WriteUInt64BigEndian(bytes, (ulong)value); break;
                case ValueDataType.Long: BinaryPrimitives.WriteInt64BigEndian(bytes, (long)value); break;
                case ValueDataType.Double: BinaryPrimitives.WriteDoubleBigEndian(bytes, (double)value); break;
            }
        }
        catch (InvalidCastException)
        {
            throw new ModbusProtocolException(
                $"Value of CLR type {value.GetType().Name} does not match the configured Modbus type '{type}'.");
        }

        var registers = new ushort[registerCount];
        for (var i = 0; i < registerCount; i++)
        {
            registers[i] = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(i * 2, 2));
        }

        if (wordOrder == ModbusWordOrder.LittleEndian)
        {
            Array.Reverse(registers);
        }

        return registers;
    }

    /// <summary>Decodes registers into a boxed value whose CLR type matches <paramref name="type"/>
    /// (the contract of ScalarVariable.SetValue).</summary>
    public static object DecodeValue(ReadOnlySpan<ushort> registers, ValueDataType type, ModbusWordOrder wordOrder)
    {
        var registerCount = RegisterCount(type);
        if (registerCount == 0)
        {
            throw new ModbusProtocolException($"Value type '{type}' cannot be mapped to Modbus registers.");
        }

        if (registers.Length != registerCount)
        {
            throw new ModbusProtocolException(
                $"Expected {registerCount} register(s) for type '{type}', got {registers.Length}.");
        }

        Span<ushort> ordered = stackalloc ushort[4];
        registers.CopyTo(ordered);
        if (wordOrder == ModbusWordOrder.LittleEndian)
        {
            ordered[..registerCount].Reverse();
        }

        Span<byte> bytes = stackalloc byte[8];
        for (var i = 0; i < registerCount; i++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(i * 2, 2), ordered[i]);
        }

        // The first arm is cast to object so the switch cannot infer 'double' as its natural type
        // (every numeric arm converts to double implicitly, which would box all values as double).
        return type switch
        {
            ValueDataType.Byte => (object)(byte)BinaryPrimitives.ReadUInt16BigEndian(bytes),
            ValueDataType.SByte => (sbyte)BinaryPrimitives.ReadInt16BigEndian(bytes),
            ValueDataType.UShort => BinaryPrimitives.ReadUInt16BigEndian(bytes),
            ValueDataType.Short => BinaryPrimitives.ReadInt16BigEndian(bytes),
            ValueDataType.UInt => BinaryPrimitives.ReadUInt32BigEndian(bytes),
            ValueDataType.Int => BinaryPrimitives.ReadInt32BigEndian(bytes),
            ValueDataType.Float => BinaryPrimitives.ReadSingleBigEndian(bytes),
            ValueDataType.ULong => BinaryPrimitives.ReadUInt64BigEndian(bytes),
            ValueDataType.Long => BinaryPrimitives.ReadInt64BigEndian(bytes),
            ValueDataType.Double => BinaryPrimitives.ReadDoubleBigEndian(bytes),
            _ => throw new ModbusProtocolException($"Value type '{type}' cannot be mapped to Modbus registers.")
        };
    }

    /// <summary>Coil/discrete-input state of a boxed numeric value: any non-zero value is ON.</summary>
    public static bool ToBit(object value)
    {
        return Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture) != 0;
    }

    /// <summary>Boxes a bit as 1/0 in the CLR type matching <paramref name="type"/>.</summary>
    public static object FromBit(bool bit, ValueDataType type)
    {
        // Cast the first arm to object — see DecodeValue for the natural-type pitfall.
        return type switch
        {
            ValueDataType.Byte => (object)(byte)(bit ? 1 : 0),
            ValueDataType.SByte => (sbyte)(bit ? 1 : 0),
            ValueDataType.UShort => (ushort)(bit ? 1 : 0),
            ValueDataType.Short => (short)(bit ? 1 : 0),
            ValueDataType.UInt => bit ? 1u : 0u,
            ValueDataType.Int => bit ? 1 : 0,
            ValueDataType.Float => bit ? 1f : 0f,
            ValueDataType.ULong => bit ? 1ul : 0ul,
            ValueDataType.Long => bit ? 1L : 0L,
            ValueDataType.Double => bit ? 1.0 : 0.0,
            _ => throw new ModbusProtocolException($"Value type '{type}' cannot be mapped to a Modbus bit.")
        };
    }
}
