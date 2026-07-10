using Qenex.QSuite.Protocols.XcpProtocol;
using ValueDataType = Qenex.QSuite.Variables.QVariables.Values.ValuesGlobal.ValueDataType;
using static Qenex.QSuite.Tests.XcpProtocolTest.Program;

namespace Qenex.QSuite.Tests.XcpProtocolTest;

internal static class CodecTests
{
    internal static void Run()
    {
        SimpleCommands_AreByteExact();
        SetMta_BothByteOrders();
        ShortUpload_BothByteOrders();
        Download_CarriesCountAndData();
        Download_RejectsInvalidLength();
        ConnectResponse_LittleEndian_Parses();
        ConnectResponse_BigEndian_Parses();
        ConnectResponse_AddressGranularity();
        ConnectResponse_Malformed_Throws();
        GetStatusResponse_Parses();
        ValueRoundTrip_AllTypes_BothByteOrders();
        EncodeValue_WrongClrType_Throws();
        DecodeValue_WrongLength_Throws();
        PacketClassification();
        ErrorDescriptions();
    }

    private static void SimpleCommands_AreByteExact()
    {
        Check(XcpCodec.BuildConnect().SequenceEqual(new byte[] { 0xFF, 0x00 }), "CONNECT = FF 00");
        Check(XcpCodec.BuildDisconnect().SequenceEqual(new byte[] { 0xFE }), "DISCONNECT = FE");
        Check(XcpCodec.BuildGetStatus().SequenceEqual(new byte[] { 0xFD }), "GET_STATUS = FD");
        Check(XcpCodec.BuildSynch().SequenceEqual(new byte[] { 0xFC }), "SYNCH = FC");
        Check(XcpCodec.BuildUpload(7).SequenceEqual(new byte[] { 0xF5, 0x07 }), "UPLOAD(7) = F5 07");
    }

    private static void SetMta_BothByteOrders()
    {
        var little = new XcpCodec { IsBigEndian = false }.BuildSetMta(2, 0x12345678);
        var big = new XcpCodec { IsBigEndian = true }.BuildSetMta(2, 0x12345678);

        Check(little.SequenceEqual(new byte[] { 0xF6, 0x00, 0x00, 0x02, 0x78, 0x56, 0x34, 0x12 }),
            "SET_MTA little-endian: F6 00 00 ext addr(LE)");
        Check(big.SequenceEqual(new byte[] { 0xF6, 0x00, 0x00, 0x02, 0x12, 0x34, 0x56, 0x78 }),
            "SET_MTA big-endian: F6 00 00 ext addr(BE)");
    }

    private static void ShortUpload_BothByteOrders()
    {
        var little = new XcpCodec { IsBigEndian = false }.BuildShortUpload(4, 1, 0xAABBCCDD);
        var big = new XcpCodec { IsBigEndian = true }.BuildShortUpload(4, 1, 0xAABBCCDD);

        Check(little.SequenceEqual(new byte[] { 0xF4, 0x04, 0x00, 0x01, 0xDD, 0xCC, 0xBB, 0xAA }),
            "SHORT_UPLOAD little-endian: F4 count 00 ext addr(LE)");
        Check(big.SequenceEqual(new byte[] { 0xF4, 0x04, 0x00, 0x01, 0xAA, 0xBB, 0xCC, 0xDD }),
            "SHORT_UPLOAD big-endian: F4 count 00 ext addr(BE)");
    }

    private static void Download_CarriesCountAndData()
    {
        var packet = XcpCodec.BuildDownload([0x11, 0x22, 0x33]);
        Check(packet.SequenceEqual(new byte[] { 0xF0, 0x03, 0x11, 0x22, 0x33 }), "DOWNLOAD = F0 count data");

        var full = XcpCodec.BuildDownload([1, 2, 3, 4, 5, 6]);
        Check(full.Length == 8 && full[1] == 6, "DOWNLOAD with 6 data bytes fills the 8-byte CTO");
    }

    private static void Download_RejectsInvalidLength()
    {
        CheckThrows<ArgumentOutOfRangeException>(() => XcpCodec.BuildDownload([]), "DOWNLOAD with 0 bytes is rejected");
        CheckThrows<ArgumentOutOfRangeException>(() => XcpCodec.BuildDownload([1, 2, 3, 4, 5, 6, 7]),
            "DOWNLOAD with 7 bytes is rejected (max 6 on classic CAN)");
    }

    private static void ConnectResponse_LittleEndian_Parses()
    {
        // FF resource=CAL|DAQ commMode=LE,AG=1 maxCto=8 maxDto=8(LE WORD) protocol=1 transport=1
        var response = XcpCodec.ParseConnectResponse([0xFF, 0x05, 0x00, 0x08, 0x08, 0x00, 0x01, 0x01]);

        Check(!response.IsBigEndian, "CONNECT LE: byte order bit 0 clear -> Intel");
        Check(response.AddressGranularity == 1, "CONNECT LE: AG bits 00 -> BYTE");
        Check(response.MaxCto == 8, "CONNECT LE: MAX_CTO");
        Check(response.MaxDto == 8, "CONNECT LE: MAX_DTO read little-endian");
        Check(response.SupportsCalibration, "CONNECT LE: CAL resource bit");
        Check(response.SupportsDaq, "CONNECT LE: DAQ resource bit");
        Check(response is { ProtocolLayerVersion: 1, TransportLayerVersion: 1 }, "CONNECT LE: version bytes");
    }

    private static void ConnectResponse_BigEndian_Parses()
    {
        // Motorola slave: byte order bit set, MAX_DTO=0x0100 encoded big-endian as 01 00.
        var response = XcpCodec.ParseConnectResponse([0xFF, 0x01, 0x01, 0x08, 0x01, 0x00, 0x01, 0x01]);

        Check(response.IsBigEndian, "CONNECT BE: byte order bit 0 set -> Motorola");
        Check(response.MaxDto == 0x0100, "CONNECT BE: MAX_DTO read big-endian");
    }

    private static void ConnectResponse_AddressGranularity()
    {
        Check(XcpCodec.ParseConnectResponse([0xFF, 0x01, 0x02, 0x08, 0x08, 0x00, 0x01, 0x01]).AddressGranularity == 2,
            "AG bits 01 -> WORD (2)");
        Check(XcpCodec.ParseConnectResponse([0xFF, 0x01, 0x04, 0x08, 0x08, 0x00, 0x01, 0x01]).AddressGranularity == 4,
            "AG bits 10 -> DWORD (4)");
        Check(XcpCodec.ParseConnectResponse([0xFF, 0x01, 0x06, 0x08, 0x08, 0x00, 0x01, 0x01]).AddressGranularity == 0,
            "AG bits 11 -> reserved (0)");
    }

    private static void ConnectResponse_Malformed_Throws()
    {
        CheckThrows<XcpProtocolException>(() => XcpCodec.ParseConnectResponse([0xFF, 0x01, 0x00]),
            "short CONNECT response is rejected");
        CheckThrows<XcpProtocolException>(() => XcpCodec.ParseConnectResponse([0xFE, 0x01, 0x00, 0x08, 0x08, 0x00, 0x01, 0x01]),
            "CONNECT response without leading 0xFF is rejected");
    }

    private static void GetStatusResponse_Parses()
    {
        var little = new XcpCodec { IsBigEndian = false }
            .ParseGetStatusResponse([0xFF, 0x40, 0x01, 0x00, 0x34, 0x12]);
        var big = new XcpCodec { IsBigEndian = true }
            .ParseGetStatusResponse([0xFF, 0x00, 0x00, 0x00, 0x12, 0x34]);

        Check(little.SessionStatus == 0x40, "GET_STATUS: session status byte");
        Check(little.IsCalibrationProtected, "GET_STATUS: CAL protection bit -> seed & key required");
        Check(little.SessionConfigurationId == 0x1234, "GET_STATUS: config id little-endian");
        Check(!big.IsCalibrationProtected, "GET_STATUS: clear protection mask -> writes allowed");
        Check(big.SessionConfigurationId == 0x1234, "GET_STATUS: config id big-endian");
    }

    private static void ValueRoundTrip_AllTypes_BothByteOrders()
    {
        (ValueDataType Type, object Value)[] samples =
        [
            (ValueDataType.Byte, (byte)0xAB),
            (ValueDataType.SByte, (sbyte)-5),
            (ValueDataType.UShort, (ushort)0xBEEF),
            (ValueDataType.Short, (short)-1234),
            (ValueDataType.UInt, 0xDEADBEEFu),
            (ValueDataType.Int, -123456),
            (ValueDataType.ULong, 0x1122334455667788ul),
            (ValueDataType.Long, -1234567890123L),
            (ValueDataType.Float, 3.14f),
            (ValueDataType.Double, -2.718281828)
        ];

        foreach (var bigEndian in new[] { false, true })
        {
            var codec = new XcpCodec { IsBigEndian = bigEndian };
            foreach (var (type, value) in samples)
            {
                var bytes = codec.EncodeValue(value, type);
                var decoded = codec.DecodeValue(bytes, type);

                Check(bytes.Length == XcpVariableSpecification.SizeOf(type),
                    $"encode {type} ({(bigEndian ? "BE" : "LE")}): correct byte count");
                Check(decoded.GetType() == value.GetType() && decoded.Equals(value),
                    $"round-trip {type} ({(bigEndian ? "BE" : "LE")}): value and CLR type preserved");
            }
        }

        // Byte-exact spot checks of the slave byte order.
        Check(new XcpCodec { IsBigEndian = false }.EncodeValue((ushort)0x1234, ValueDataType.UShort)
                .SequenceEqual(new byte[] { 0x34, 0x12 }), "ushort little-endian memory image");
        Check(new XcpCodec { IsBigEndian = true }.EncodeValue((ushort)0x1234, ValueDataType.UShort)
                .SequenceEqual(new byte[] { 0x12, 0x34 }), "ushort big-endian memory image");
    }

    private static void EncodeValue_WrongClrType_Throws()
    {
        CheckThrows<XcpProtocolException>(
            () => new XcpCodec().EncodeValue(1.5, ValueDataType.Float),
            "boxed double encoded as Float is rejected (CLR type must match)");
    }

    private static void DecodeValue_WrongLength_Throws()
    {
        CheckThrows<XcpProtocolException>(
            () => new XcpCodec().DecodeValue([1, 2], ValueDataType.Float),
            "decoding Float from 2 bytes is rejected");
    }

    private static void PacketClassification()
    {
        Check(XcpPacket.Classify(0xFF) == XcpPacketKind.Response, "PID 0xFF -> RES");
        Check(XcpPacket.Classify(0xFE) == XcpPacketKind.Error, "PID 0xFE -> ERR");
        Check(XcpPacket.Classify(0xFD) == XcpPacketKind.Event, "PID 0xFD -> EV");
        Check(XcpPacket.Classify(0xFC) == XcpPacketKind.ServiceRequest, "PID 0xFC -> SERV");
        Check(XcpPacket.Classify(0xFB) == XcpPacketKind.DaqDto, "PID 0xFB -> DAQ DTO");
        Check(XcpPacket.Classify(0x00) == XcpPacketKind.DaqDto, "PID 0x00 -> DAQ DTO");
    }

    private static void ErrorDescriptions()
    {
        Check(XcpErrorCode.Describe(0x25).Contains("seed & key"), "ERR_ACCESS_LOCKED mentions seed & key");
        Check(XcpErrorCode.Describe(0x00).Contains("ERR_CMD_SYNCH"), "ERR_CMD_SYNCH is described");
        Check(XcpErrorCode.Describe(0x99).Contains("0x99"), "unknown code falls back to hex");
    }
}
