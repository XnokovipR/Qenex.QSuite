using Qenex.QSuite.Protocols.XcpCore;
using static Qenex.QSuite.Tests.XcpProtocolTest.Program;

namespace Qenex.QSuite.Tests.XcpProtocolTest;

internal static class EthernetFramerTests
{
    internal static void Run()
    {
        Encode_HeaderIsLenCtrLittleEndian();
        Encode_CounterIncrementsAndResets();
        Decode_WholeFrame();
        Decode_ChunkedAndCoalescedFrames();
        Decode_ZeroLen_DropsBufferAndThrows();
    }

    private static void Encode_HeaderIsLenCtrLittleEndian()
    {
        var framer = new XcpEthernetFramer();

        var frame = framer.EncodeFrame([0xF4, 0x04, 0x00, 0x01, 0xDD, 0xCC, 0xBB, 0xAA]);

        Check(frame.Length == 12, "encode: 4-byte header + packet");
        Check(frame[0] == 0x08 && frame[1] == 0x00, "encode: LEN little-endian");
        Check(frame[2] == 0x00 && frame[3] == 0x00, "encode: first CTR is 0");
        Check(frame.Skip(4).SequenceEqual(new byte[] { 0xF4, 0x04, 0x00, 0x01, 0xDD, 0xCC, 0xBB, 0xAA }),
            "encode: packet follows the header unchanged");
    }

    private static void Encode_CounterIncrementsAndResets()
    {
        var framer = new XcpEthernetFramer();

        framer.EncodeFrame([0xFF]);
        var second = framer.EncodeFrame([0xFF]);
        Check(second[2] == 0x01 && second[3] == 0x00, "encode: CTR increments per frame");

        framer.Reset();
        var afterReset = framer.EncodeFrame([0xFF]);
        Check(afterReset[2] == 0x00, "encode: Reset restarts the CTR for a new session");
    }

    private static void Decode_WholeFrame()
    {
        var framer = new XcpEthernetFramer();

        framer.Append([0x02, 0x00, 0x2A, 0x00, 0xFF, 0x42]);

        Check(framer.TryDequeuePacket(out var packet) && packet.SequenceEqual(new byte[] { 0xFF, 0x42 }),
            "decode: packet extracted, LEN/CTR header stripped (slave CTR ignored)");
        Check(!framer.TryDequeuePacket(out _), "decode: buffer empty afterwards");
    }

    private static void Decode_ChunkedAndCoalescedFrames()
    {
        var framer = new XcpEthernetFramer();

        // First frame split mid-header and mid-payload, second frame glued to the remainder —
        // exactly what a TCP stream may deliver.
        framer.Append([0x03, 0x00, 0x01]);
        Check(!framer.TryDequeuePacket(out _), "decode chunked: incomplete header+payload waits");
        framer.Append([0x00, 0xFF, 0x11]);
        Check(!framer.TryDequeuePacket(out _), "decode chunked: payload still one byte short");
        framer.Append([0x22, 0x01, 0x00, 0x02, 0x00, 0xFE]);

        Check(framer.TryDequeuePacket(out var first) && first.SequenceEqual(new byte[] { 0xFF, 0x11, 0x22 }),
            "decode chunked: first packet assembled across three chunks");
        Check(framer.TryDequeuePacket(out var second) && second.SequenceEqual(new byte[] { 0xFE }),
            "decode chunked: coalesced second packet extracted from the same buffer");
    }

    private static void Decode_ZeroLen_DropsBufferAndThrows()
    {
        var framer = new XcpEthernetFramer();
        framer.Append([0x00, 0x00, 0x00, 0x00, 0xFF]);

        CheckThrows<XcpProtocolException>(() => framer.TryDequeuePacket(out _),
            "decode: LEN=0 is malformed and throws");

        framer.Append([0x01, 0x00, 0x00, 0x00, 0xFF]);
        Check(framer.TryDequeuePacket(out var packet) && packet.Length == 1,
            "decode: buffer was dropped, a fresh valid frame parses again");
    }
}
