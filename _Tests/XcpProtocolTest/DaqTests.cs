using System.Buffers.Binary;
using Qenex.QSuite.Protocols.XcpCore;
using XcpTcpSessionSettings = Qenex.QSuite.Protocols.XcpTcpProtocol.XcpTcpSessionSettings;
using Qenex.QSuite.Variables.VariableEvents;
using static Qenex.QSuite.Tests.XcpProtocolTest.Program;

namespace Qenex.QSuite.Tests.XcpProtocolTest;

/// <summary>DAQ phase 4 (decisions D1–D6): eventExtraParams parsing, DAQ command codec, the DTO
/// decoder and the master's configuration sequence against a scripted slave.</summary>
internal static class DaqTests
{
    internal static void Run()
    {
        ExtraParams_Validation();
        Codec_DaqCommands_ByteExact();
        Codec_DaqResponses_Parsed();
        Decoder_OdtFillDaqWord_WithTimestamp();
        Decoder_DropsAndOverload();
        Decoder_AbsolutePid();
        Mapper_SlaveSpacingWrapAndFallbacks();
        Settings_DaqTimestamps();
        Master_ConfigureAndStartDaq_Sequence().GetAwaiter().GetResult();
        Master_GetDaqEventInfo_UploadsName().GetAwaiter().GetResult();
    }

    #region eventExtraParams

    private static void ExtraParams_Validation()
    {
        Check(Channel("") == null, "extraParams: empty string -> ordinary event");
        Check(Channel("   ") == null, "extraParams: whitespace -> ordinary event");
        Check(Channel("direction=\"DAQ\";daqId=\"3\"") == 3, "extraParams: DAQ + daqId parsed");
        Check(Channel("DAQID=\"7\";DIRECTION=\"daq\"") == 7, "extraParams: keys and value are case-insensitive");
        Check(Channel("foo=\"1\"") == null, "extraParams: unknown key alone -> warning only, still an ordinary event");

        CheckThrows<ArgumentException>(() => Channel("direction=\"DAQ\""),
            "extraParams: direction without daqId is rejected");
        CheckThrows<ArgumentException>(() => Channel("daqId=\"3\""),
            "extraParams: daqId without direction is rejected");
        CheckThrows<ArgumentException>(() => Channel("direction=\"STIM\";daqId=\"3\""),
            "extraParams: unsupported direction is rejected");
        CheckThrows<ArgumentException>(() => Channel("direction=\"DAQ\";daqId=\"70000\""),
            "extraParams: daqId out of 0..65535 is rejected");
        CheckThrows<ArgumentException>(() => Channel("DAQ"),
            "extraParams: a segment without key=value form is rejected");
    }

    private static ushort? Channel(string extraParams)
    {
        return XcpEventExtraParams.GetDaqChannel(
            new PeriodicVarEvent { Name = "e", EventExtraParams = extraParams });
    }

    #endregion

    #region Codec

    private static void Codec_DaqCommands_ByteExact()
    {
        var codec = new XcpCodec(); // little-endian

        Check(XcpCodec.BuildFreeDaq().SequenceEqual(new byte[] { 0xD6 }), "codec: FREE_DAQ");
        Check(codec.BuildAllocDaq(2).SequenceEqual(new byte[] { 0xD5, 0x00, 0x02, 0x00 }), "codec: ALLOC_DAQ");
        Check(codec.BuildAllocOdt(1, 3).SequenceEqual(new byte[] { 0xD4, 0x00, 0x01, 0x00, 0x03 }), "codec: ALLOC_ODT");
        Check(codec.BuildAllocOdtEntry(1, 2, 4).SequenceEqual(new byte[] { 0xD3, 0x00, 0x01, 0x00, 0x02, 0x04 }),
            "codec: ALLOC_ODT_ENTRY");
        Check(codec.BuildSetDaqPtr(1, 2, 0).SequenceEqual(new byte[] { 0xE2, 0x00, 0x01, 0x00, 0x02, 0x00 }),
            "codec: SET_DAQ_PTR");
        Check(codec.BuildWriteDaq(8, 1, 0x12345678).SequenceEqual(new byte[] { 0xE1, 0xFF, 0x08, 0x01, 0x78, 0x56, 0x34, 0x12 }),
            "codec: WRITE_DAQ (bit offset 0xFF = whole element)");
        Check(codec.BuildSetDaqListMode(0x10, 1, 2, 1, 0).SequenceEqual(new byte[] { 0xE0, 0x10, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00 }),
            "codec: SET_DAQ_LIST_MODE");
        Check(codec.BuildStartStopDaqList(XcpDaqStartStopMode.Select, 1).SequenceEqual(new byte[] { 0xDE, 0x02, 0x01, 0x00 }),
            "codec: START_STOP_DAQ_LIST");
        Check(XcpCodec.BuildStartStopSynch(XcpDaqSynchMode.StartSelected).SequenceEqual(new byte[] { 0xDD, 0x01 }),
            "codec: START_STOP_SYNCH");
        Check(codec.BuildGetDaqEventInfo(3).SequenceEqual(new byte[] { 0xD7, 0x00, 0x03, 0x00 }),
            "codec: GET_DAQ_EVENT_INFO");

        var bigEndian = new XcpCodec { IsBigEndian = true };
        Check(bigEndian.BuildAllocDaq(2).SequenceEqual(new byte[] { 0xD5, 0x00, 0x00, 0x02 }),
            "codec: WORD fields follow the slave's byte order (Motorola)");
    }

    private static void Codec_DaqResponses_Parsed()
    {
        var codec = new XcpCodec();

        // Dynamic lists, timestamps, overload via PID MSB, ODT+FIL+DAQW identification, ext per DAQ.
        var processor = codec.ParseDaqProcessorInfoResponse([0xFF, 0x51, 0x00, 0x00, 0x02, 0x00, 0x00, 0xF0]);
        Check(processor is
              {
                  HasDynamicLists: true, SupportsTimestamps: true, OverloadIndicationByPid: true,
                  MaxDaq: 0, MaxEventChannel: 2, DtoHeaderSize: 4,
                  IdentificationType: XcpDaqIdentificationType.OdtWithFillAndDaqWord
              },
            "codec: GET_DAQ_PROCESSOR_INFO parsed (properties, key byte, header size)");

        // Fixed 4-byte timestamps, unit 1 ms, 1 tick; max ODT entry 8 bytes.
        var resolution = codec.ParseDaqResolutionInfoResponse([0xFF, 0x01, 0x08, 0x01, 0x08, 0x6C, 0x01, 0x00]);
        Check(resolution is { MaxOdtEntrySizeDaq: 8, TimestampSize: 4, TimestampFixed: true, TimestampTicks: 1 },
            "codec: GET_DAQ_RESOLUTION_INFO parsed (timestamp mode decomposed)");
        Check(Math.Abs(resolution.TimestampTickSeconds - 1e-3) < 1e-12,
            "codec: tick duration = TICKS x UNIT (1 tick of 1 ms)");

        // XCPlite on a 1 us clock reports unit 1 ns with 1000 ticks -> 1 us per tick (TICKS x UNIT,
        // never UNIT / TICKS - the inverse reading froze the DAQ time axis on live hardware).
        var xcplite = codec.ParseDaqResolutionInfoResponse([0xFF, 0x01, 0xF8, 0x01, 0xF8, 0x0C, 0xE8, 0x03]);
        Check(Math.Abs(xcplite.TimestampTickSeconds - 1e-6) < 1e-15,
            "codec: XCPlite-style resolution (1 ns unit, 1000 ticks) = 1 us per tick");

        var eventInfo = XcpCodec.ParseDaqEventInfoResponse([0xFF, 0x84, 0xFF, 0x04, 0x01, 0x06, 0x00]);
        Check(eventInfo is { SupportsDaq: true, NameLength: 4, CycleTimeMs: 1.0 },
            "codec: GET_DAQ_EVENT_INFO parsed (cycle 1×10^6 ns = 1 ms)");

        var sporadic = XcpCodec.ParseDaqEventInfoResponse([0xFF, 0x04, 0xFF, 0x00, 0x00, 0x00, 0x00]);
        Check(sporadic.CycleTimeMs == null, "codec: TIME_CYCLE 0 -> sporadic event (no nominal cycle)");

        Check(XcpCodec.ParseStartStopDaqListResponse([0xFF, 0x05]) == 5, "codec: FIRST_PID extracted");
        Check(XcpCodec.ParseStartStopDaqListResponse([0xFF]) == 0, "codec: missing FIRST_PID tolerated as 0");
    }

    #endregion

    #region DTO decoder

    private static readonly List<XcpDaqListPlan> TwoOdtPlan =
    [
        new XcpDaqListPlan(1, [
            new List<XcpDaqEntryPlan> { new(4, 0, 0x1000), new(2, 0, 0x2000) }, // ODT 0
            new List<XcpDaqEntryPlan> { new(8, 0, 0x3000) }                     // ODT 1
        ])
    ];

    private static void Decoder_OdtFillDaqWord_WithTimestamp()
    {
        var decoder = new XcpDaqDecoder(XcpDaqIdentificationType.OdtWithFillAndDaqWord, isBigEndian: false,
            timestampSizeOdt0: 4, overloadIndicationByPid: true, TwoOdtPlan);
        var entries = new List<XcpDaqDecoder.DecodedEntry>();

        // ODT 0: header(4) + timestamp(4) + 4B + 2B.
        byte[] odt0 = [0x00, 0xAA, 0x00, 0x00, 1, 2, 3, 4, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66];
        Check(decoder.TryDecode(odt0, entries, out var ts0), "decoder: ODT 0 with timestamp decodes");
        Check(entries is [{ OdtIndex: 0, EntryIndex: 0, DataOffset: 8, Size: 4 }, { EntryIndex: 1, DataOffset: 12, Size: 2 }],
            "decoder: ODT 0 entries sit after the extracted timestamp");
        Check(ts0 == 0x04030201, "decoder: ODT 0 timestamp extracted (little-endian raw ticks)");

        // ODT 1: header(4) + 8B, no timestamp outside ODT 0.
        byte[] odt1 = [0x01, 0xAA, 0x00, 0x00, 1, 2, 3, 4, 5, 6, 7, 8];
        Check(decoder.TryDecode(odt1, entries, out var ts1), "decoder: ODT 1 decodes");
        Check(entries is [{ OdtIndex: 1, EntryIndex: 0, DataOffset: 4, Size: 8 }] && ts1 == null,
            "decoder: only ODT 0 carries the timestamp");
    }

    private static void Decoder_DropsAndOverload()
    {
        var decoder = new XcpDaqDecoder(XcpDaqIdentificationType.OdtWithFillAndDaqWord, isBigEndian: false,
            timestampSizeOdt0: 4, overloadIndicationByPid: true, TwoOdtPlan);
        var entries = new List<XcpDaqDecoder.DecodedEntry>();

        Check(!decoder.TryDecode([0x00, 0xAA], entries, out _), "decoder: packet shorter than the header is dropped");
        Check(!decoder.TryDecode([0x00, 0xAA, 0x05, 0x00, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6], entries, out _),
            "decoder: unknown DAQ list number is dropped");
        Check(!decoder.TryDecode([0x07, 0xAA, 0x00, 0x00, 1, 2, 3, 4], entries, out _),
            "decoder: unknown ODT number is dropped");
        Check(!decoder.TryDecode([0x00, 0xAA, 0x00, 0x00, 0, 0, 0, 0, 1, 2], entries, out _),
            "decoder: truncated data part is dropped");
        Check(!decoder.TryDecode([0x00, 0xAA, 0x00, 0x00, 1, 2], entries, out _),
            "decoder: packet shorter than its ODT 0 timestamp is dropped");
        Check(decoder.DroppedPackets == 5, "decoder: drops are counted");

        // Overload: MSB of the relative ODT byte set — packet still decodes as ODT 1.
        byte[] overloaded = [0x81, 0xAA, 0x00, 0x00, 1, 2, 3, 4, 5, 6, 7, 8];
        Check(decoder.TryDecode(overloaded, entries, out _) && entries is [{ OdtIndex: 1 }],
            "decoder: overload MSB is stripped and the packet still decodes");
    }

    private static void Decoder_AbsolutePid()
    {
        var decoder = new XcpDaqDecoder(XcpDaqIdentificationType.AbsolutePid, isBigEndian: false,
            timestampSizeOdt0: 0, overloadIndicationByPid: false, TwoOdtPlan);
        var entries = new List<XcpDaqDecoder.DecodedEntry>();

        Check(!decoder.TryDecode([0x10, 1, 2, 3, 4, 0x11, 0x22], entries, out _),
            "decoder: absolute PID without FIRST_PID knowledge is dropped");

        decoder.SetFirstPids([0x10]);
        Check(decoder.TryDecode([0x11, 1, 2, 3, 4, 5, 6, 7, 8], entries, out _) &&
              entries is [{ ListIndex: 0, OdtIndex: 1, DataOffset: 1, Size: 8 }],
            "decoder: absolute PID maps through FIRST_PID to list and relative ODT");
        Check(!decoder.TryDecode([0x30, 1, 2], entries, out _), "decoder: PID outside every list range is dropped");
    }

    #endregion

    #region Timestamp mapper (4b/4c)

    private static void Mapper_SlaveSpacingWrapAndFallbacks()
    {
        var t0 = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        var mapper = new XcpDaqTimestampMapper(timestampSize: 4, tickSeconds: 1e-6, listCount: 2);

        // Anchor: the first timestamped sample takes its receive time; from then on the
        // spacing follows the slave clock even when TCP delivers in bursts.
        Check(mapper.Map(0, 1_000_000, t0) == t0, "mapper: first sample anchors to the receive time");
        var second = mapper.Map(0, 1_010_000, t0.AddMilliseconds(48));
        Check(CloseTo(second, t0.AddMilliseconds(10)), "mapper: spacing follows the slave clock, not the arrival burst");

        // ODTs 1..n have no timestamp: they reuse the cycle time of their list; a list that has
        // not seen any ODT 0 yet falls back to the receive time.
        Check(mapper.Map(0, null, t0.AddMilliseconds(49)) == second, "mapper: timestamp-less ODT reuses the cycle time");
        Check(mapper.Map(1, null, t0.AddMilliseconds(50)) == t0.AddMilliseconds(50),
            "mapper: a list without any ODT 0 yet uses the receive time");

        // Counter overflow: a smaller raw value continues the time axis (32-bit unwrap).
        var wrapMapper = new XcpDaqTimestampMapper(timestampSize: 4, tickSeconds: 1e-6, listCount: 1);
        Check(wrapMapper.Map(0, uint.MaxValue - 4_999, t0) == t0, "mapper: pre-wrap sample anchors");
        var afterWrap = wrapMapper.Map(0, 5_000, t0.AddMilliseconds(12));
        Check(CloseTo(afterWrap, t0.AddMilliseconds(10)), "mapper: 32-bit counter wrap is unwrapped");

        // Slave restart: the counter jump maps far away from the receive time -> re-anchor.
        var restarted = mapper.Map(0, 500, t0.AddSeconds(5));
        Check(restarted == t0.AddSeconds(5), "mapper: slave restart re-anchors to the receive time");
    }

    private static bool CloseTo(DateTime actual, DateTime expected)
    {
        return Math.Abs((actual - expected).TotalMilliseconds) < 0.001;
    }

    private static void Settings_DaqTimestamps()
    {
        Check(XcpTcpSessionSettings.Parse("timeoutMs=1000").UseSlaveDaqTimestamps,
            "settings: daqTimestamps defaults to slave");
        Check(XcpTcpSessionSettings.Parse("daqTimestamps=\"SLAVE\"").UseSlaveDaqTimestamps,
            "settings: daqTimestamps=slave parsed (case-insensitive)");
        Check(!XcpTcpSessionSettings.Parse("daqTimestamps=master").UseSlaveDaqTimestamps,
            "settings: daqTimestamps=master parsed");
        CheckThrows<ArgumentException>(() => XcpTcpSessionSettings.Parse("daqTimestamps=ecu"),
            "settings: unknown daqTimestamps value is rejected");
    }

    #endregion

    #region Master DAQ sequence

    /// <summary>Scripted slave for the master-level DAQ tests (same pattern as MasterTests).</summary>
    private sealed class FakeDaqSlave
    {
        public readonly List<byte[]> Sent = [];
        public readonly XcpMaster Master = new();
        public Func<byte[], byte[]?>? Responder;

        public FakeDaqSlave()
        {
            Master.TimeoutMs = 100;
            Master.Transmitter = (packet, _) =>
            {
                lock (Sent)
                {
                    Sent.Add(packet);
                }

                var response = Responder?.Invoke(packet);
                if (response != null)
                {
                    Master.OnPacketReceived(response);
                }

                return Task.CompletedTask;
            };
        }

        public static byte[]? DefaultResponder(byte[] command) => command[0] switch
        {
            XcpCommand.Connect => [0xFF, 0x05, 0x00, 0xFA, 0xFF, 0x05, 0x01, 0x01],
            XcpCommand.GetStatus => [0xFF, 0x00, 0x00, 0x00, 0x00, 0x00],
            XcpCommand.FreeDaq or XcpCommand.AllocDaq or XcpCommand.AllocOdt or XcpCommand.AllocOdtEntry
                or XcpCommand.SetDaqPtr or XcpCommand.WriteDaq or XcpCommand.SetDaqListMode
                or XcpCommand.StartStopSynch => [0xFF],
            XcpCommand.StartStopDaqList => [0xFF, 0x05],
            _ => null
        };
    }

    private static async Task Master_ConfigureAndStartDaq_Sequence()
    {
        var slave = new FakeDaqSlave { Responder = FakeDaqSlave.DefaultResponder };
        await slave.Master.ConnectAsync();
        slave.Sent.Clear();

        List<XcpDaqListPlan> plans =
        [
            new XcpDaqListPlan(7, [
                new List<XcpDaqEntryPlan> { new(4, 0, 0x1000), new(2, 1, 0x2000) },
                new List<XcpDaqEntryPlan> { new(8, 0, 0x3000) }
            ])
        ];

        var firstPids = await slave.Master.ConfigureAndStartDaqAsync(plans, includeTimestamp: true);

        Check(firstPids is [0x05], "daq sequence: FIRST_PID collected from START_STOP_DAQ_LIST");

        var pids = slave.Sent.Select(p => p[0]).ToArray();
        byte[] expected =
        [
            XcpCommand.FreeDaq, XcpCommand.AllocDaq, XcpCommand.AllocOdt,
            XcpCommand.AllocOdtEntry, XcpCommand.AllocOdtEntry,
            XcpCommand.SetDaqPtr, XcpCommand.WriteDaq, XcpCommand.WriteDaq,
            XcpCommand.SetDaqPtr, XcpCommand.WriteDaq,
            XcpCommand.SetDaqListMode, XcpCommand.StartStopDaqList, XcpCommand.StartStopSynch
        ];
        Check(pids.SequenceEqual(expected),
            $"daq sequence: exact command order on the wire (got {string.Join(",", pids.Select(p => p.ToString("X2")))})");

        Check(slave.Sent[1].SequenceEqual(new byte[] { 0xD5, 0x00, 0x01, 0x00 }), "daq sequence: ALLOC_DAQ for 1 list");
        Check(slave.Sent[2].SequenceEqual(new byte[] { 0xD4, 0x00, 0x00, 0x00, 0x02 }), "daq sequence: ALLOC_ODT 2 ODTs");
        Check(slave.Sent[3].SequenceEqual(new byte[] { 0xD3, 0x00, 0x00, 0x00, 0x00, 0x02 }),
            "daq sequence: ALLOC_ODT_ENTRY ODT0 = 2 entries");
        Check(slave.Sent[6].SequenceEqual(new byte[] { 0xE1, 0xFF, 0x04, 0x00, 0x00, 0x10, 0x00, 0x00 }),
            "daq sequence: WRITE_DAQ carries size/ext/address");
        Check(slave.Sent[10].SequenceEqual(new byte[] { 0xE0, 0x10, 0x00, 0x00, 0x07, 0x00, 0x01, 0x00 }),
            "daq sequence: SET_DAQ_LIST_MODE with timestamp bit, channel 7, prescaler 1");
        Check(slave.Sent[11].SequenceEqual(new byte[] { 0xDE, 0x02, 0x00, 0x00 }),
            "daq sequence: START_STOP_DAQ_LIST mode=select");
        Check(slave.Sent[12].SequenceEqual(new byte[] { 0xDD, 0x01 }),
            "daq sequence: START_STOP_SYNCH starts the selected lists");
    }

    private static async Task Master_GetDaqEventInfo_UploadsName()
    {
        var slave = new FakeDaqSlave();
        slave.Responder = cmd => cmd[0] switch
        {
            XcpCommand.GetDaqEventInfo => [0xFF, 0x04, 0xFF, 0x04, 0x01, 0x06, 0x00],
            XcpCommand.Upload => [0xFF, (byte)'l', (byte)'o', (byte)'o', (byte)'p'],
            _ => FakeDaqSlave.DefaultResponder(cmd)
        };
        await slave.Master.ConnectAsync();
        slave.Sent.Clear();

        var (info, name) = await slave.Master.GetDaqEventInfoAsync(1);

        Check(name == "loop", "daq event info: channel name uploaded via the MTA left by the command");
        Check(info.CycleTimeMs == 1.0, "daq event info: nominal cycle decoded");
        Check(slave.Sent.Count == 2 && slave.Sent[0].SequenceEqual(new byte[] { 0xD7, 0x00, 0x01, 0x00 }) &&
              slave.Sent[1].SequenceEqual(new byte[] { 0xF5, 0x04 }),
            "daq event info: GET_DAQ_EVENT_INFO followed by UPLOAD(nameLength)");
    }

    #endregion
}
