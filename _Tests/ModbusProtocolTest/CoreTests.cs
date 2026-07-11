using Qenex.QSuite.Protocols.Modbus;
using ValueDataType = Qenex.QSuite.Variables.QVariables.Values.ValuesGlobal.ValueDataType;
using static Qenex.QSuite.Tests.ModbusProtocolTest.Program;

namespace Qenex.QSuite.Tests.ModbusProtocolTest;

internal static class CoreTests
{
    internal static void Run()
    {
        Crc_KnownVector();
        Pdu_ReadRequest_ByteExact();
        Pdu_WriteRequests_ByteExact();
        Pdu_ReadResponses_Parse();
        Pdu_WriteResponse_Validation();
        Pdu_RequestParsing_SlaveSide();
        RtuFramer_EncodeAndDecode_MasterRole();
        RtuFramer_ChunkedAndGarbage_Resync();
        RtuFramer_SlaveRole_ParsesRequests();
        TcpFramer_RoundTrip_And_Fragmentation();
        RegisterCodec_RoundTrip_AllTypes_BothWordOrders();
        RegisterCodec_ByteExact_Float();
        RegisterCodec_Bits();
    }

    private static void Crc_KnownVector()
    {
        // Canonical example from the Modbus spec: 01 04 02 FF FF -> wire bytes B8 80 (low first),
        // i.e. CRC value 0x80B8.
        var crc = ModbusCrc.Compute([0x01, 0x04, 0x02, 0xFF, 0xFF]);
        Check(crc == 0x80B8, $"CRC16 known vector (got 0x{crc:X4}, expected 0x80B8)");
    }

    private static void Pdu_ReadRequest_ByteExact()
    {
        var pdu = ModbusPdu.BuildReadRequest(ModbusFunction.ReadHoldingRegisters, 0x006B, 3);
        Check(pdu.SequenceEqual(new byte[] { 0x03, 0x00, 0x6B, 0x00, 0x03 }), "FC03 request = 03 006B 0003");
    }

    private static void Pdu_WriteRequests_ByteExact()
    {
        Check(ModbusPdu.BuildWriteSingleCoil(0x00AC, true).SequenceEqual(new byte[] { 0x05, 0x00, 0xAC, 0xFF, 0x00 }),
            "FC05 request = 05 00AC FF00");
        Check(ModbusPdu.BuildWriteSingleRegister(0x0001, 0x0003).SequenceEqual(new byte[] { 0x06, 0x00, 0x01, 0x00, 0x03 }),
            "FC06 request = 06 0001 0003");
        Check(ModbusPdu.BuildWriteMultipleRegisters(0x0001, [0x000A, 0x0102])
                .SequenceEqual(new byte[] { 0x10, 0x00, 0x01, 0x00, 0x02, 0x04, 0x00, 0x0A, 0x01, 0x02 }),
            "FC16 request = 10 0001 0002 04 000A 0102");
        CheckThrows<ArgumentOutOfRangeException>(() => ModbusPdu.BuildWriteMultipleRegisters(0, new ushort[124]),
            "FC16 request rejects more than 123 registers");
    }

    private static void Pdu_ReadResponses_Parse()
    {
        // FC01: 10 coils -> 2 bytes, LSB-first: CD 01 = 1,0,1,1 0,0,1,1 + 1,0.
        var bits = ModbusPdu.ParseReadBitsResponse([0x01, 0x02, 0xCD, 0x01], 10);
        Check(bits.SequenceEqual(new[] { true, false, true, true, false, false, true, true, true, false }),
            "FC01 response bits unpack LSB-first");

        var registers = ModbusPdu.ParseReadRegistersResponse([0x03, 0x04, 0x02, 0x2B, 0x01, 0x06], 2);
        Check(registers.SequenceEqual(new ushort[] { 0x022B, 0x0106 }), "FC03 response registers parse big-endian");

        CheckThrows<ModbusProtocolException>(() => ModbusPdu.ParseReadRegistersResponse([0x03, 0x04, 0x02, 0x2B], 2),
            "truncated FC03 response is rejected");
    }

    private static void Pdu_WriteResponse_Validation()
    {
        var request = ModbusPdu.BuildWriteSingleRegister(5, 77);
        ModbusPdu.ValidateWriteResponse(request, request); // echo — must not throw
        Check(true, "FC06 echo response validates");

        CheckThrows<ModbusProtocolException>(
            () => ModbusPdu.ValidateWriteResponse(request, ModbusPdu.BuildWriteSingleRegister(5, 78)),
            "FC06 response with different value is rejected");
    }

    private static void Pdu_RequestParsing_SlaveSide()
    {
        var read = ModbusPdu.ParseRequest([0x03, 0x00, 0x10, 0x00, 0x02]);
        Check(read is { Function: 0x03, Address: 0x10, Count: 2 }, "slave parses FC03 request");

        var write = ModbusPdu.ParseRequest([0x10, 0x00, 0x01, 0x00, 0x02, 0x04, 0x00, 0x0A, 0x01, 0x02]);
        Check(write is { Function: 0x10, Address: 1, Count: 2 } && write.Data.SequenceEqual(new byte[] { 0x00, 0x0A, 0x01, 0x02 }),
            "slave parses FC16 request incl. data");

        CheckThrows<ModbusProtocolException>(() => ModbusPdu.ParseRequest([0x10, 0x00, 0x01, 0x00, 0x02, 0x05]),
            "FC16 request with wrong byte count is rejected");
    }

    private static void RtuFramer_EncodeAndDecode_MasterRole()
    {
        var framer = new ModbusRtuFramer(ModbusFramerRole.Master);

        var frame = framer.Encode(new ModbusAdu(0x01, [0x04, 0x02, 0xFF, 0xFF]));
        Check(frame.SequenceEqual(new byte[] { 0x01, 0x04, 0x02, 0xFF, 0xFF, 0xB8, 0x80 }),
            "RTU encode appends unit id and CRC (low byte first)");

        framer.Append(frame);
        Check(framer.TryDequeueFrame(out var adu) && adu.UnitId == 1 && adu.Pdu.SequenceEqual(new byte[] { 0x04, 0x02, 0xFF, 0xFF }),
            "RTU decode round-trips the encoded frame");
        Check(!framer.TryDequeueFrame(out _), "RTU buffer empty after dequeue");
    }

    private static void RtuFramer_ChunkedAndGarbage_Resync()
    {
        var framer = new ModbusRtuFramer(ModbusFramerRole.Master);
        var frame = framer.Encode(new ModbusAdu(0x11, [0x03, 0x02, 0x12, 0x34]));

        // Garbage prefix + frame split into single-byte chunks.
        framer.Append([0xDE, 0xAD]);
        foreach (var b in frame)
        {
            framer.Append([b]);
        }

        Check(framer.TryDequeueFrame(out var adu) && adu.UnitId == 0x11 && adu.Pdu[0] == 0x03,
            "RTU framer resynchronizes past garbage and reassembles chunked frames");

        // Exception response (5-byte frame).
        var exceptionFrame = framer.Encode(new ModbusAdu(0x11, [0x83, 0x02]));
        framer.Append(exceptionFrame);
        Check(framer.TryDequeueFrame(out var error) && error.IsException && error.ExceptionCode == 0x02,
            "RTU framer handles exception responses");
    }

    private static void RtuFramer_SlaveRole_ParsesRequests()
    {
        var framer = new ModbusRtuFramer(ModbusFramerRole.Slave);

        var readRequest = framer.Encode(new ModbusAdu(0x01, ModbusPdu.BuildReadRequest(0x03, 0, 2)));
        var writeRequest = framer.Encode(new ModbusAdu(0x01, ModbusPdu.BuildWriteMultipleRegisters(0, [1, 2])));

        framer.Append(readRequest);
        framer.Append(writeRequest);

        Check(framer.TryDequeueFrame(out var first) && first.Function == 0x03, "slave framer parses FC03 request");
        Check(framer.TryDequeueFrame(out var second) && second.Function == 0x10 && second.Pdu.Length == 10,
            "slave framer parses variable-length FC16 request");
    }

    private static void TcpFramer_RoundTrip_And_Fragmentation()
    {
        var framer = new ModbusTcpFramer();

        var frame = framer.Encode(new ModbusAdu(0xFF, [0x03, 0x00, 0x10, 0x00, 0x02], 0x1234));
        Check(frame.SequenceEqual(new byte[] { 0x12, 0x34, 0x00, 0x00, 0x00, 0x06, 0xFF, 0x03, 0x00, 0x10, 0x00, 0x02 }),
            "TCP encode builds the MBAP header (transaction, protocol 0, length, unit)");

        // Two frames delivered fragmented across three chunks.
        var second = framer.Encode(new ModbusAdu(0x01, [0x06, 0x00, 0x01, 0x00, 0x05], 0x1235));
        var stream = frame.Concat(second).ToArray();
        framer.Append(stream.AsSpan(0, 5));
        framer.Append(stream.AsSpan(5, 9));
        framer.Append(stream.AsSpan(14));

        Check(framer.TryDequeueFrame(out var one) && one.TransactionId == 0x1234 && one.UnitId == 0xFF,
            "TCP framer reassembles the first fragmented frame");
        Check(framer.TryDequeueFrame(out var two) && two.TransactionId == 0x1235 && two.Function == 0x06,
            "TCP framer yields the second frame from the same stream");
    }

    private static void RegisterCodec_RoundTrip_AllTypes_BothWordOrders()
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

        foreach (var wordOrder in new[] { ModbusWordOrder.BigEndian, ModbusWordOrder.LittleEndian })
        {
            foreach (var (type, value) in samples)
            {
                var registers = ModbusRegisterCodec.EncodeValue(value, type, wordOrder);
                var decoded = ModbusRegisterCodec.DecodeValue(registers, type, wordOrder);

                Check(registers.Length == ModbusRegisterCodec.RegisterCount(type),
                    $"register codec {type} ({wordOrder}): correct register count");
                Check(decoded.GetType() == value.GetType() && decoded.Equals(value),
                    $"register codec round-trip {type} ({wordOrder}): value and CLR type preserved");
            }
        }
    }

    private static void RegisterCodec_ByteExact_Float()
    {
        // 123.456f = 0x42F6E979.
        var big = ModbusRegisterCodec.EncodeValue(123.456f, ValueDataType.Float, ModbusWordOrder.BigEndian);
        var little = ModbusRegisterCodec.EncodeValue(123.456f, ValueDataType.Float, ModbusWordOrder.LittleEndian);

        Check(big.SequenceEqual(new ushort[] { 0x42F6, 0xE979 }), "float big word order: MSW first");
        Check(little.SequenceEqual(new ushort[] { 0xE979, 0x42F6 }), "float little word order: LSW first");
    }

    private static void RegisterCodec_Bits()
    {
        Check(ModbusRegisterCodec.ToBit((ushort)5), "non-zero value -> bit ON");
        Check(!ModbusRegisterCodec.ToBit(0.0), "zero value -> bit OFF");
        Check(ModbusRegisterCodec.FromBit(true, ValueDataType.UShort) is (ushort)1, "bit ON -> boxed ushort 1");
        Check(ModbusRegisterCodec.FromBit(false, ValueDataType.Float) is 0f, "bit OFF -> boxed float 0");
    }
}
