using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Protocols.XcpCore;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.Variables.VariableEvents;
using ValueDataType = Qenex.QSuite.Variables.QVariables.Values.ValuesGlobal.ValueDataType;
using static Qenex.QSuite.Tests.XcpProtocolTest.Program;

namespace Qenex.QSuite.Tests.XcpProtocolTest;

/// <summary>STIM unit tests (S1–S7): DTO building for all identification field types, the
/// STIM fields of GET_DAQ_RESOLUTION_INFO, mode bits and the S2 configuration validation.</summary>
internal static class StimTests
{
    internal static void Run()
    {
        Encoder_IdentificationFieldTypes_ByteExact();
        Encoder_Timestamp_Layouts();
        Codec_ResolutionInfo_StimFields();
        Specification_StimEvent_Validation();
    }

    private static void Encoder_IdentificationFieldTypes_ByteExact()
    {
        byte[] payload = [0xAA, 0xBB];

        var absolute = new XcpStimEncoder(XcpDaqIdentificationType.AbsolutePid, false, 0);
        Check(absolute.BuildDto(5, odtIndex: 2, firstPid: 0x10, null, payload)
                .SequenceEqual(new byte[] { 0x12, 0xAA, 0xBB }),
            "stim encoder: absolute PID = FIRST_PID + relative ODT");

        var daqByte = new XcpStimEncoder(XcpDaqIdentificationType.OdtWithDaqByte, false, 0);
        Check(daqByte.BuildDto(5, 1, 0x99, null, payload)
                .SequenceEqual(new byte[] { 0x01, 0x05, 0xAA, 0xBB }),
            "stim encoder: ODT + DAQ byte (FIRST_PID ignored)");

        var daqWord = new XcpStimEncoder(XcpDaqIdentificationType.OdtWithDaqWord, false, 0);
        Check(daqWord.BuildDto(0x0102, 3, 0, null, payload)
                .SequenceEqual(new byte[] { 0x03, 0x02, 0x01, 0xAA, 0xBB }),
            "stim encoder: ODT + DAQ word little-endian");

        var daqWordBig = new XcpStimEncoder(XcpDaqIdentificationType.OdtWithDaqWord, true, 0);
        Check(daqWordBig.BuildDto(0x0102, 3, 0, null, payload)
                .SequenceEqual(new byte[] { 0x03, 0x01, 0x02, 0xAA, 0xBB }),
            "stim encoder: DAQ word follows the slave's byte order (Motorola)");

        var filled = new XcpStimEncoder(XcpDaqIdentificationType.OdtWithFillAndDaqWord, false, 0);
        Check(filled.BuildDto(7, 0, 0, null, payload)
                .SequenceEqual(new byte[] { 0x00, 0x00, 0x07, 0x00, 0xAA, 0xBB }),
            "stim encoder: ODT + fill + DAQ word (aligned)");
    }

    private static void Encoder_Timestamp_Layouts()
    {
        byte[] payload = [0x55];

        var ts4 = new XcpStimEncoder(XcpDaqIdentificationType.OdtWithDaqByte, false, 4);
        Check(ts4.BuildDto(0, 0, 0, 0x11223344, payload)
                .SequenceEqual(new byte[] { 0x00, 0x00, 0x44, 0x33, 0x22, 0x11, 0x55 }),
            "stim encoder: 4-byte ODT-0 timestamp little-endian");
        Check(ts4.BuildDto(0, 1, 0, null, payload)
                .SequenceEqual(new byte[] { 0x01, 0x00, 0x55 }),
            "stim encoder: ODT 1..n carries no timestamp even on a timestamped list");

        var ts2 = new XcpStimEncoder(XcpDaqIdentificationType.OdtWithDaqByte, false, 2);
        Check(ts2.BuildDto(0, 0, 0, 0xA1B2, payload)
                .SequenceEqual(new byte[] { 0x00, 0x00, 0xB2, 0xA1, 0x55 }),
            "stim encoder: 2-byte timestamp");

        var ts1 = new XcpStimEncoder(XcpDaqIdentificationType.OdtWithDaqByte, false, 1);
        Check(ts1.BuildDto(0, 0, 0, 0x7F, payload)
                .SequenceEqual(new byte[] { 0x00, 0x00, 0x7F, 0x55 }),
            "stim encoder: 1-byte timestamp");

        var ts4Big = new XcpStimEncoder(XcpDaqIdentificationType.OdtWithDaqByte, true, 4);
        Check(ts4Big.BuildDto(0, 0, 0, 0x11223344, payload)
                .SequenceEqual(new byte[] { 0x00, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 }),
            "stim encoder: timestamp follows the slave's byte order (Motorola)");
    }

    private static void Codec_ResolutionInfo_StimFields()
    {
        var codec = new XcpCodec();

        var resolution = codec.ParseDaqResolutionInfoResponse([0xFF, 0x01, 0xFA, 0x02, 0x08, 0x6C, 0x01, 0x00]);
        Check(resolution is
              {
                  GranularityOdtEntrySizeDaq: 1, MaxOdtEntrySizeDaq: 0xFA,
                  GranularityOdtEntrySizeStim: 2, MaxOdtEntrySizeStim: 8,
                  TimestampSize: 4, TimestampFixed: true
              },
            "codec: GET_DAQ_RESOLUTION_INFO carries the STIM granularity and max entry size (bytes 3-4)");

        Check(XcpDaqListModeBits.Direction == 0x02 && XcpDaqListModeBits.Timestamp == 0x10,
            "constants: SET_DAQ_LIST_MODE direction bit is bit 1, timestamp bit 4 (spec 1.6.4.1.1.3)");
    }

    private static void Specification_StimEvent_Validation()
    {
        var stimEvent = new PeriodicVarEvent
        {
            Name = "stim10ms", Period = 10, Unit = TimeUnit.Milisec,
            EventExtraParams = "direction=\"STIM\";daqId=\"2\""
        };
        var variable = new ScalarVariable
        {
            Id = 1, Name = "SinValue",
            Values = new Values<double> { Value = 0d, ValueType = ValueDataType.Double }
        };

        var spec = XcpVariableSpecification.Create(
            "address=\"0x3000\";direction=\"write\";eventRef=\"stim10ms\"", [stimEvent], variable);
        Check(spec is { IsStimEvent: true, DaqEventChannel: 2, Direction: CommDirection.Write },
            "spec: write variable binds to a STIM event (S2)");

        CheckThrows<ArgumentException>(() => XcpVariableSpecification.Create(
                "address=\"0x3000\";direction=\"read\";eventRef=\"stim10ms\"", [stimEvent], variable),
            "spec: a read variable on a STIM event is rejected (S2)");
        CheckThrows<ArgumentException>(() => XcpVariableSpecification.Create(
                "address=\"0x3000\";direction=\"readWrite\";eventRef=\"stim10ms\"", [stimEvent], variable),
            "spec: a readWrite variable on a STIM event is rejected (S2)");
    }
}
