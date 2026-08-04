using System.Buffers.Binary;

namespace Qenex.QSuite.Protocols.XcpCore;

/// <summary>
/// XCP on Ethernet framing (ASAM XCP 1.1 Part 3): every XCP packet travels as
/// | LEN (2 B) | CTR (2 B) | packet (LEN bytes) | with no tail. LEN and CTR are always
/// little-endian (Intel format) regardless of the slave's byte order. TCP delivers arbitrary
/// chunks, so received bytes are accumulated until a whole frame is available (same reassembly
/// approach as ModbusTcpFramer). Master and slave each keep their own CTR sequence; the received
/// CTR is not validated — TCP cannot reorder or drop frames. Not thread-safe; the owner
/// serializes access.
/// </summary>
public sealed class XcpEthernetFramer
{
    private const int HeaderLength = 4;

    private readonly List<byte> buffer = [];
    private ushort sendCounter;

    /// <summary>Wraps one outgoing XCP packet into its wire frame and advances the send counter.</summary>
    public byte[] EncodeFrame(byte[] packet)
    {
        var frame = new byte[HeaderLength + packet.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(frame, (ushort)packet.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(2), sendCounter++);
        packet.CopyTo(frame.AsSpan(HeaderLength));
        return frame;
    }

    /// <summary>Appends a received chunk to the reassembly buffer.</summary>
    public void Append(ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            buffer.Add(value);
        }
    }

    /// <summary>
    /// Extracts the next complete XCP packet (without the LEN/CTR header), if any. LEN 0 cannot
    /// occur in a valid stream and leaves no way to find the next frame boundary — buffered bytes
    /// are dropped and an <see cref="XcpProtocolException"/> raised; the session layer recovers
    /// through its normal timeout path (SYNCH, retry, reconnect).
    /// </summary>
    public bool TryDequeuePacket(out byte[] packet)
    {
        if (buffer.Count >= HeaderLength)
        {
            var length = buffer[0] | (buffer[1] << 8);
            if (length == 0)
            {
                buffer.Clear();
                throw new XcpProtocolException("Malformed XCP on Ethernet frame (LEN=0); buffered bytes dropped.");
            }

            if (buffer.Count >= HeaderLength + length)
            {
                packet = new byte[length];
                buffer.CopyTo(HeaderLength, packet, 0, length);
                buffer.RemoveRange(0, HeaderLength + length);
                return true;
            }
        }

        packet = null!;
        return false;
    }

    /// <summary>Starts a new session: drops buffered bytes and restarts the send counter.</summary>
    public void Reset()
    {
        buffer.Clear();
        sendCounter = 0;
    }
}
