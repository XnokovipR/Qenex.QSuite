using System.Buffers.Binary;
using ValueDataType = Qenex.QSuite.Variables.QVariables.Values.ValuesGlobal.ValueDataType;

namespace Qenex.QSuite.Protocols.XcpCore;

/// <summary>
/// Transport-agnostic XCP packet codec. Works purely on XCP packets (PID + data bytes) — framing
/// (CAN identifiers today, LEN/CTR headers on TCP later) is the transport adapter's job. Multi-byte
/// command parameters and values are emitted in the slave's byte order (<see cref="IsBigEndian"/>,
/// learned from the CONNECT response); the CONNECT command itself has no multi-byte fields, so the
/// initial value does not matter for connecting. Only address granularity BYTE (AG=1) is supported.
/// </summary>
public sealed class XcpCodec
{
    /// <summary>Slave byte order from CONNECT COMM_MODE_BASIC bit 0: false = Intel, true = Motorola.</summary>
    public bool IsBigEndian { get; set; }

    #region Command builders

    public static byte[] BuildConnect(byte mode = 0x00) => [XcpCommand.Connect, mode];

    public static byte[] BuildDisconnect() => [XcpCommand.Disconnect];

    public static byte[] BuildGetStatus() => [XcpCommand.GetStatus];

    public static byte[] BuildSynch() => [XcpCommand.Synch];

    /// <summary>SET_MTA: 0xF6 | reserved WORD | address extension | address DWORD.</summary>
    public byte[] BuildSetMta(byte addressExtension, uint address)
    {
        var packet = new byte[8];
        packet[0] = XcpCommand.SetMta;
        packet[3] = addressExtension;
        WriteUInt32(packet.AsSpan(4), address);
        return packet;
    }

    /// <summary>SHORT_UPLOAD: 0xF4 | element count | reserved | address extension | address DWORD.</summary>
    public byte[] BuildShortUpload(byte count, byte addressExtension, uint address)
    {
        var packet = new byte[8];
        packet[0] = XcpCommand.ShortUpload;
        packet[1] = count;
        packet[3] = addressExtension;
        WriteUInt32(packet.AsSpan(4), address);
        return packet;
    }

    /// <summary>UPLOAD: 0xF5 | element count. Reads from the current MTA and post-increments it.</summary>
    public static byte[] BuildUpload(byte count) => [XcpCommand.Upload, count];

    /// <summary>DOWNLOAD: 0xF0 | element count | data. Writes to the current MTA and post-increments it.
    /// Keeping the packet within the slave's MAX_CTO (e.g. 6 data bytes on classic CAN) is the
    /// caller's contract — XcpMaster chunks by the limit learned from the CONNECT response.</summary>
    public static byte[] BuildDownload(ReadOnlySpan<byte> data)
    {
        if (data.Length is < 1 or > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(data), data.Length,
                "A single DOWNLOAD packet carries 1..255 data bytes.");
        }

        var packet = new byte[2 + data.Length];
        packet[0] = XcpCommand.Download;
        packet[1] = (byte)data.Length;
        data.CopyTo(packet.AsSpan(2));
        return packet;
    }

    #endregion

    #region Response parsers

    /// <summary>
    /// Parses the CONNECT positive response (full packet including the leading 0xFF). Static because
    /// the slave byte order is not known until this very response: MAX_DTO is read using the byte
    /// order announced in the same packet.
    /// </summary>
    public static XcpConnectResponse ParseConnectResponse(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 8 || packet[0] != 0xFF)
        {
            throw new XcpProtocolException($"Malformed CONNECT response ({packet.Length} bytes).");
        }

        var commModeBasic = packet[2];
        var isBigEndian = (commModeBasic & 0x01) != 0;
        var addressGranularity = ((commModeBasic >> 1) & 0x03) switch
        {
            0 => 1,
            1 => 2,
            2 => 4,
            _ => 0 // reserved
        };

        var maxDto = isBigEndian
            ? BinaryPrimitives.ReadUInt16BigEndian(packet[4..6])
            : BinaryPrimitives.ReadUInt16LittleEndian(packet[4..6]);

        return new XcpConnectResponse(
            Resource: packet[1],
            IsBigEndian: isBigEndian,
            AddressGranularity: addressGranularity,
            MaxCto: packet[3],
            MaxDto: maxDto,
            ProtocolLayerVersion: packet[6],
            TransportLayerVersion: packet[7]);
    }

    /// <summary>Parses the GET_STATUS positive response (full packet including the leading 0xFF).</summary>
    public XcpStatusResponse ParseGetStatusResponse(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 6 || packet[0] != 0xFF)
        {
            throw new XcpProtocolException($"Malformed GET_STATUS response ({packet.Length} bytes).");
        }

        var sessionConfigurationId = IsBigEndian
            ? BinaryPrimitives.ReadUInt16BigEndian(packet[4..6])
            : BinaryPrimitives.ReadUInt16LittleEndian(packet[4..6]);

        return new XcpStatusResponse(
            SessionStatus: packet[1],
            ResourceProtection: packet[2],
            SessionConfigurationId: sessionConfigurationId);
    }

    #endregion

    #region Value <-> memory bytes

    /// <summary>
    /// Encodes a boxed scalar value into its ECU memory image in the slave's byte order. The boxed
    /// CLR type must match <paramref name="type"/> (the same contract as ScalarVariable.SetValue).
    /// </summary>
    public byte[] EncodeValue(object value, ValueDataType type)
    {
        var bytes = new byte[XcpVariableSpecification.SizeOf(type)];
        if (bytes.Length == 0)
        {
            throw new XcpProtocolException($"Value type '{type}' cannot be transferred over XCP.");
        }

        try
        {
            switch (type)
            {
                case ValueDataType.Byte: bytes[0] = (byte)value; break;
                case ValueDataType.SByte: bytes[0] = unchecked((byte)(sbyte)value); break;
                case ValueDataType.UShort: BinaryPrimitives.WriteUInt16LittleEndian(bytes, (ushort)value); break;
                case ValueDataType.Short: BinaryPrimitives.WriteInt16LittleEndian(bytes, (short)value); break;
                case ValueDataType.UInt: BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)value); break;
                case ValueDataType.Int: BinaryPrimitives.WriteInt32LittleEndian(bytes, (int)value); break;
                case ValueDataType.ULong: BinaryPrimitives.WriteUInt64LittleEndian(bytes, (ulong)value); break;
                case ValueDataType.Long: BinaryPrimitives.WriteInt64LittleEndian(bytes, (long)value); break;
                case ValueDataType.Float: BinaryPrimitives.WriteSingleLittleEndian(bytes, (float)value); break;
                case ValueDataType.Double: BinaryPrimitives.WriteDoubleLittleEndian(bytes, (double)value); break;
                default: throw new XcpProtocolException($"Value type '{type}' cannot be transferred over XCP.");
            }
        }
        catch (InvalidCastException)
        {
            throw new XcpProtocolException(
                $"Value of CLR type {value.GetType().Name} does not match the configured XCP type '{type}'.");
        }

        if (IsBigEndian)
        {
            Array.Reverse(bytes);
        }

        return bytes;
    }

    /// <summary>
    /// Decodes an ECU memory image (in the slave's byte order) into a boxed scalar value whose CLR
    /// type matches <paramref name="type"/>, so it can be assigned via ScalarVariable.SetValue.
    /// </summary>
    public object DecodeValue(ReadOnlySpan<byte> bytes, ValueDataType type)
    {
        var size = XcpVariableSpecification.SizeOf(type);
        if (size == 0)
        {
            throw new XcpProtocolException($"Value type '{type}' cannot be transferred over XCP.");
        }

        if (bytes.Length != size)
        {
            throw new XcpProtocolException($"Expected {size} bytes for type '{type}', got {bytes.Length}.");
        }

        Span<byte> buffer = stackalloc byte[8];
        bytes.CopyTo(buffer);
        if (IsBigEndian)
        {
            buffer[..size].Reverse();
        }

        // The first arm is cast to object so the switch cannot infer 'double' as its natural type
        // (every numeric arm converts to double implicitly, which would box all values as double).
        return type switch
        {
            ValueDataType.Byte => (object)buffer[0],
            ValueDataType.SByte => (sbyte)buffer[0],
            ValueDataType.UShort => BinaryPrimitives.ReadUInt16LittleEndian(buffer),
            ValueDataType.Short => BinaryPrimitives.ReadInt16LittleEndian(buffer),
            ValueDataType.UInt => BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            ValueDataType.Int => BinaryPrimitives.ReadInt32LittleEndian(buffer),
            ValueDataType.ULong => BinaryPrimitives.ReadUInt64LittleEndian(buffer),
            ValueDataType.Long => BinaryPrimitives.ReadInt64LittleEndian(buffer),
            ValueDataType.Float => BinaryPrimitives.ReadSingleLittleEndian(buffer),
            ValueDataType.Double => BinaryPrimitives.ReadDoubleLittleEndian(buffer),
            _ => throw new XcpProtocolException($"Value type '{type}' cannot be transferred over XCP.")
        };
    }

    private void WriteUInt32(Span<byte> destination, uint value)
    {
        if (IsBigEndian)
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
        }
    }

    #endregion
}
