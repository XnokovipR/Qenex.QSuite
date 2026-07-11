using System.Buffers.Binary;

namespace Qenex.QSuite.Protocols.Modbus;

/// <summary>Which side of the link a framer parses: a master receives responses, a slave receives
/// requests. RTU needs this to compute the deterministic frame length from the function code.</summary>
public enum ModbusFramerRole
{
    Master,
    Slave
}

/// <summary>
/// Turns a byte stream into Modbus ADUs and back. Byte-stream transports (serial port, TCP) deliver
/// arbitrary chunks — a framer accumulates them, finds frame boundaries and validates integrity.
/// Implementations are NOT thread-safe; the owner serializes access.
/// </summary>
public interface IModbusFramer
{
    /// <summary>True when the framing carries a transaction id end to end (Modbus TCP). RTU has
    /// none — responses are correlated by unit id and strict request/response alternation.</summary>
    bool SupportsTransactionIds { get; }

    /// <summary>Wraps an ADU into its wire form (MBAP header or CRC-terminated RTU frame).</summary>
    byte[] Encode(ModbusAdu adu);

    /// <summary>Appends a received chunk to the reassembly buffer.</summary>
    void Append(ReadOnlySpan<byte> data);

    /// <summary>Extracts the next complete, valid frame from the buffer, if any.</summary>
    bool TryDequeueFrame(out ModbusAdu adu);

    /// <summary>Drops any partially received bytes (used for timeout recovery).</summary>
    void Reset();
}

/// <summary>
/// Modbus TCP framing: MBAP header | transaction(2) | protocol=0(2) | length(2) | unit(1) | + PDU.
/// </summary>
public sealed class ModbusTcpFramer : IModbusFramer
{
    private const int HeaderLength = 7;
    private const int MaxPduLength = 253;

    private readonly List<byte> buffer = [];

    public bool SupportsTransactionIds => true;

    public byte[] Encode(ModbusAdu adu)
    {
        var frame = new byte[HeaderLength + adu.Pdu.Length];
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(0), adu.TransactionId);
        // Protocol identifier (bytes 2-3) stays 0 = Modbus.
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(4), (ushort)(1 + adu.Pdu.Length));
        frame[6] = adu.UnitId;
        adu.Pdu.CopyTo(frame.AsSpan(HeaderLength));
        return frame;
    }

    public void Append(ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            buffer.Add(value);
        }
    }

    public bool TryDequeueFrame(out ModbusAdu adu)
    {
        while (buffer.Count >= HeaderLength)
        {
            var protocolId = (ushort)((buffer[2] << 8) | buffer[3]);
            var length = (ushort)((buffer[4] << 8) | buffer[5]);

            if (protocolId != 0 || length is < 2 or > 1 + MaxPduLength)
            {
                // Not a Modbus frame start — resynchronize by sliding one byte.
                buffer.RemoveAt(0);
                continue;
            }

            var totalLength = HeaderLength - 1 + length;
            if (buffer.Count < totalLength)
            {
                break; // incomplete — wait for more bytes
            }

            var transactionId = (ushort)((buffer[0] << 8) | buffer[1]);
            var unitId = buffer[6];
            var pdu = new byte[length - 1];
            buffer.CopyTo(HeaderLength, pdu, 0, pdu.Length);
            buffer.RemoveRange(0, totalLength);

            adu = new ModbusAdu(unitId, pdu, transactionId);
            return true;
        }

        adu = null!;
        return false;
    }

    public void Reset()
    {
        buffer.Clear();
    }
}

/// <summary>
/// Modbus RTU framing: | unit(1) | PDU | CRC16(2, low byte first) |. Instead of relying on the
/// spec's 3.5-character silent interval — which USB/RS-485 converters and virtual COM ports do not
/// preserve — frames are delimited by the deterministic length each supported function code
/// implies, then validated by CRC; on any mismatch the buffer slides one byte to resynchronize.
/// </summary>
public sealed class ModbusRtuFramer(ModbusFramerRole role) : IModbusFramer
{
    private readonly List<byte> buffer = [];

    public bool SupportsTransactionIds => false;

    public byte[] Encode(ModbusAdu adu)
    {
        var frame = new byte[1 + adu.Pdu.Length + 2];
        frame[0] = adu.UnitId;
        adu.Pdu.CopyTo(frame.AsSpan(1));
        var crc = ModbusCrc.Compute(frame.AsSpan(0, frame.Length - 2));
        frame[^2] = (byte)(crc & 0xFF); // low byte first on the wire
        frame[^1] = (byte)(crc >> 8);
        return frame;
    }

    public void Append(ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            buffer.Add(value);
        }
    }

    public bool TryDequeueFrame(out ModbusAdu adu)
    {
        while (buffer.Count >= 4) // minimum frame: unit + fc + at least 0 data + crc(2) → exceptions have 4+1
        {
            var frameLength = ExpectedFrameLength();
            if (frameLength < 0)
            {
                buffer.RemoveAt(0); // unknown function code — resynchronize
                continue;
            }

            if (frameLength == 0 || buffer.Count < frameLength)
            {
                break; // need more bytes to decide or to complete the frame
            }

            var frame = new byte[frameLength];
            buffer.CopyTo(0, frame, 0, frameLength);

            var crc = ModbusCrc.Compute(frame.AsSpan(0, frameLength - 2));
            var wireCrc = (ushort)(frame[^2] | (frame[^1] << 8));
            if (crc != wireCrc)
            {
                buffer.RemoveAt(0); // corrupted or mid-stream start — resynchronize
                continue;
            }

            buffer.RemoveRange(0, frameLength);
            adu = new ModbusAdu(frame[0], frame[1..^2]);
            return true;
        }

        adu = null!;
        return false;
    }

    public void Reset()
    {
        buffer.Clear();
    }

    /// <summary>Total frame length implied by the function code at the buffer head, for the
    /// configured role. Returns 0 when more bytes are needed to decide, -1 for unknown codes.</summary>
    private int ExpectedFrameLength()
    {
        var function = buffer[1];

        if ((function & ModbusFunction.ExceptionFlag) != 0)
        {
            // Exception responses: unit + fc + code + crc. Only ever received by a master.
            return role == ModbusFramerRole.Master ? 5 : -1;
        }

        return role == ModbusFramerRole.Master
            ? function switch
            {
                // Responses: unit + fc + byteCount + data + crc
                ModbusFunction.ReadCoils or ModbusFunction.ReadDiscreteInputs or
                ModbusFunction.ReadHoldingRegisters or ModbusFunction.ReadInputRegisters
                    => buffer.Count < 3 ? 0 : 3 + buffer[2] + 2,
                // Write echoes: unit + fc + address(2) + value/count(2) + crc
                ModbusFunction.WriteSingleCoil or ModbusFunction.WriteSingleRegister or
                ModbusFunction.WriteMultipleRegisters => 8,
                _ => -1
            }
            : function switch
            {
                // Requests: unit + fc + address(2) + count/value(2) + crc
                ModbusFunction.ReadCoils or ModbusFunction.ReadDiscreteInputs or
                ModbusFunction.ReadHoldingRegisters or ModbusFunction.ReadInputRegisters or
                ModbusFunction.WriteSingleCoil or ModbusFunction.WriteSingleRegister => 8,
                // FC 16 request: unit + fc + address(2) + count(2) + byteCount + data + crc
                ModbusFunction.WriteMultipleRegisters => buffer.Count < 7 ? 0 : 7 + buffer[6] + 2,
                _ => -1
            };
    }
}
