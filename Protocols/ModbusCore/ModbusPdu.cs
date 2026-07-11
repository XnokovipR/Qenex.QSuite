using System.Buffers.Binary;

namespace Qenex.QSuite.Protocols.Modbus;

/// <summary>Parsed Modbus request as seen by a slave (server).</summary>
public sealed record ModbusRequest(byte Function, ushort Address, ushort Count, byte[] Data);

/// <summary>
/// Framing-independent PDU codec (function code + data). Multi-byte fields inside a PDU are always
/// big-endian per the Modbus specification. Builders/parsers exist for both roles: a master builds
/// requests and parses responses, a slave parses requests and builds responses.
/// </summary>
public static class ModbusPdu
{
    /// <summary>Maximum bit count of one FC 01/02 read (spec limit 0x7D0).</summary>
    public const int MaxBitsPerRead = 2000;

    /// <summary>Maximum register count of one FC 03/04 read (spec limit 0x7D).</summary>
    public const int MaxRegistersPerRead = 125;

    /// <summary>Maximum register count of one FC 16 write (spec limit 0x7B).</summary>
    public const int MaxRegistersPerWrite = 123;

    #region Master: requests

    /// <summary>FC 01/02/03/04: | fc | address(2) | count(2) |.</summary>
    public static byte[] BuildReadRequest(byte function, ushort address, ushort count)
    {
        var pdu = new byte[5];
        pdu[0] = function;
        BinaryPrimitives.WriteUInt16BigEndian(pdu.AsSpan(1), address);
        BinaryPrimitives.WriteUInt16BigEndian(pdu.AsSpan(3), count);
        return pdu;
    }

    /// <summary>FC 05: | 05 | address(2) | 0xFF00 on / 0x0000 off |.</summary>
    public static byte[] BuildWriteSingleCoil(ushort address, bool value)
    {
        var pdu = new byte[5];
        pdu[0] = ModbusFunction.WriteSingleCoil;
        BinaryPrimitives.WriteUInt16BigEndian(pdu.AsSpan(1), address);
        BinaryPrimitives.WriteUInt16BigEndian(pdu.AsSpan(3), value ? (ushort)0xFF00 : (ushort)0x0000);
        return pdu;
    }

    /// <summary>FC 06: | 06 | address(2) | value(2) |.</summary>
    public static byte[] BuildWriteSingleRegister(ushort address, ushort value)
    {
        var pdu = new byte[5];
        pdu[0] = ModbusFunction.WriteSingleRegister;
        BinaryPrimitives.WriteUInt16BigEndian(pdu.AsSpan(1), address);
        BinaryPrimitives.WriteUInt16BigEndian(pdu.AsSpan(3), value);
        return pdu;
    }

    /// <summary>FC 16: | 10 | address(2) | count(2) | byteCount | registers… |.</summary>
    public static byte[] BuildWriteMultipleRegisters(ushort address, ReadOnlySpan<ushort> values)
    {
        if (values.Length is < 1 or > MaxRegistersPerWrite)
        {
            throw new ArgumentOutOfRangeException(nameof(values), values.Length,
                $"FC 16 writes 1..{MaxRegistersPerWrite} registers.");
        }

        var pdu = new byte[6 + values.Length * 2];
        pdu[0] = ModbusFunction.WriteMultipleRegisters;
        BinaryPrimitives.WriteUInt16BigEndian(pdu.AsSpan(1), address);
        BinaryPrimitives.WriteUInt16BigEndian(pdu.AsSpan(3), (ushort)values.Length);
        pdu[5] = (byte)(values.Length * 2);
        for (var i = 0; i < values.Length; i++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(pdu.AsSpan(6 + i * 2), values[i]);
        }

        return pdu;
    }

    #endregion

    #region Master: responses

    /// <summary>FC 01/02 response: | fc | byteCount | bits… | — bits packed LSB-first.</summary>
    public static bool[] ParseReadBitsResponse(ReadOnlySpan<byte> pdu, int expectedCount)
    {
        var expectedBytes = (expectedCount + 7) / 8;
        if (pdu.Length < 2 + expectedBytes || pdu[1] != expectedBytes)
        {
            throw new ModbusProtocolException(
                $"Malformed read-bits response (function 0x{(pdu.Length > 0 ? pdu[0] : 0):X2}, {pdu.Length} bytes).");
        }

        var bits = new bool[expectedCount];
        for (var i = 0; i < expectedCount; i++)
        {
            bits[i] = (pdu[2 + i / 8] & (1 << (i % 8))) != 0;
        }

        return bits;
    }

    /// <summary>FC 03/04 response: | fc | byteCount | registers… |.</summary>
    public static ushort[] ParseReadRegistersResponse(ReadOnlySpan<byte> pdu, int expectedCount)
    {
        if (pdu.Length < 2 + expectedCount * 2 || pdu[1] != expectedCount * 2)
        {
            throw new ModbusProtocolException(
                $"Malformed read-registers response (function 0x{(pdu.Length > 0 ? pdu[0] : 0):X2}, {pdu.Length} bytes).");
        }

        var registers = new ushort[expectedCount];
        for (var i = 0; i < expectedCount; i++)
        {
            registers[i] = BinaryPrimitives.ReadUInt16BigEndian(pdu.Slice(2 + i * 2, 2));
        }

        return registers;
    }

    /// <summary>FC 05/06 echo the request; FC 16 echoes address + count. Validates the write took.</summary>
    public static void ValidateWriteResponse(ReadOnlySpan<byte> requestPdu, ReadOnlySpan<byte> responsePdu)
    {
        // All supported write responses repeat the first 5 request bytes (fc, address, value/count).
        if (responsePdu.Length < 5 || !responsePdu[..5].SequenceEqual(requestPdu[..5]))
        {
            throw new ModbusProtocolException(
                $"Write response does not match the request (function 0x{(responsePdu.Length > 0 ? responsePdu[0] : 0):X2}).");
        }
    }

    #endregion

    #region Slave: requests & responses

    /// <summary>Parses a request PDU into its common shape. For FC 16 <c>Data</c> carries the raw
    /// register bytes; for FC 05/06 <c>Count</c> carries the written value.</summary>
    public static ModbusRequest ParseRequest(ReadOnlySpan<byte> pdu)
    {
        if (pdu.Length < 5)
        {
            throw new ModbusProtocolException($"Request PDU too short ({pdu.Length} bytes).");
        }

        var function = pdu[0];
        var address = BinaryPrimitives.ReadUInt16BigEndian(pdu.Slice(1, 2));
        var countOrValue = BinaryPrimitives.ReadUInt16BigEndian(pdu.Slice(3, 2));

        if (function == ModbusFunction.WriteMultipleRegisters)
        {
            if (pdu.Length < 6 || pdu[5] != countOrValue * 2 || pdu.Length < 6 + pdu[5])
            {
                throw new ModbusProtocolException("Malformed FC 16 request.");
            }

            return new ModbusRequest(function, address, countOrValue, pdu.Slice(6, pdu[5]).ToArray());
        }

        return new ModbusRequest(function, address, countOrValue, []);
    }

    public static byte[] BuildReadBitsResponse(byte function, IReadOnlyList<bool> bits)
    {
        var byteCount = (bits.Count + 7) / 8;
        var pdu = new byte[2 + byteCount];
        pdu[0] = function;
        pdu[1] = (byte)byteCount;
        for (var i = 0; i < bits.Count; i++)
        {
            if (bits[i])
            {
                pdu[2 + i / 8] |= (byte)(1 << (i % 8));
            }
        }

        return pdu;
    }

    public static byte[] BuildReadRegistersResponse(byte function, IReadOnlyList<ushort> registers)
    {
        var pdu = new byte[2 + registers.Count * 2];
        pdu[0] = function;
        pdu[1] = (byte)(registers.Count * 2);
        for (var i = 0; i < registers.Count; i++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(pdu.AsSpan(2 + i * 2), registers[i]);
        }

        return pdu;
    }

    /// <summary>FC 05/06/16 positive responses echo the first 5 bytes of the request.</summary>
    public static byte[] BuildWriteEchoResponse(ReadOnlySpan<byte> requestPdu)
    {
        return requestPdu[..5].ToArray();
    }

    public static byte[] BuildExceptionResponse(byte function, byte exceptionCode)
    {
        return [(byte)(function | ModbusFunction.ExceptionFlag), exceptionCode];
    }

    #endregion
}
