using System.Buffers.Binary;

namespace Qenex.QSuite.Protocols.XcpCore;

/// <summary>
/// Builds STIM DTO packets sent master → slave (the mirror of <see cref="XcpDaqDecoder"/>).
/// The identification field uses the same type the slave announced in DAQ_KEY_BYTE for its own
/// DAQ packets (spec: "the master has to use the same type … when transferring STIM packets"),
/// and the optional ODT-0 timestamp field the same layout as in DAQ direction. STIM PIDs live in
/// the 0x00..0xBF range — the absolute-PID validity check is the planner's job.
/// </summary>
public sealed class XcpStimEncoder(
    XcpDaqIdentificationType identificationType,
    bool isBigEndian,
    int timestampSizeOdt0)
{
    /// <summary>Identification field bytes at the start of every STIM DTO.</summary>
    public int HeaderSize => identificationType switch
    {
        XcpDaqIdentificationType.AbsolutePid => 1,
        XcpDaqIdentificationType.OdtWithDaqByte => 2,
        XcpDaqIdentificationType.OdtWithDaqWord => 3,
        _ => 4
    };

    /// <summary>Extra bytes the ODT-0 packet of a cycle carries (timestamped STIM lists only).</summary>
    public int TimestampSize => timestampSizeOdt0;

    /// <summary>
    /// Builds one STIM DTO. <paramref name="firstPid"/> is the slave-assigned FIRST_PID of the
    /// list (absolute-PID identification only, ignored otherwise); <paramref name="timestampRaw"/>
    /// must be provided exactly for ODT 0 of a timestamped list and null otherwise.
    /// </summary>
    public byte[] BuildDto(ushort daqListNumber, byte odtIndex, byte firstPid, uint? timestampRaw,
        ReadOnlySpan<byte> payload)
    {
        var timestampSize = timestampRaw.HasValue ? timestampSizeOdt0 : 0;
        var packet = new byte[HeaderSize + timestampSize + payload.Length];

        switch (identificationType)
        {
            case XcpDaqIdentificationType.AbsolutePid:
                packet[0] = (byte)(firstPid + odtIndex);
                break;

            case XcpDaqIdentificationType.OdtWithDaqByte:
                packet[0] = odtIndex;
                packet[1] = (byte)daqListNumber;
                break;

            case XcpDaqIdentificationType.OdtWithDaqWord:
                packet[0] = odtIndex;
                WriteUInt16(packet.AsSpan(1, 2), daqListNumber);
                break;

            default: // OdtWithFillAndDaqWord
                packet[0] = odtIndex;
                packet[1] = 0x00; // fill byte for alignment, value unspecified by the spec
                WriteUInt16(packet.AsSpan(2, 2), daqListNumber);
                break;
        }

        if (timestampRaw is { } timestamp)
        {
            WriteTimestamp(packet.AsSpan(HeaderSize, timestampSize), timestamp);
        }

        payload.CopyTo(packet.AsSpan(HeaderSize + timestampSize));
        return packet;
    }

    private void WriteTimestamp(Span<byte> destination, uint value)
    {
        switch (destination.Length)
        {
            case 1:
                destination[0] = (byte)value;
                break;
            case 2:
                WriteUInt16(destination, (ushort)value);
                break;
            default:
                if (isBigEndian)
                {
                    BinaryPrimitives.WriteUInt32BigEndian(destination, value);
                }
                else
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
                }

                break;
        }
    }

    private void WriteUInt16(Span<byte> destination, ushort value)
    {
        if (isBigEndian)
        {
            BinaryPrimitives.WriteUInt16BigEndian(destination, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt16LittleEndian(destination, value);
        }
    }
}
