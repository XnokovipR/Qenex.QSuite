using Qenex.QSuite.Protocols.Modbus;
using static Qenex.QSuite.Tests.ModbusProtocolTest.Program;

namespace Qenex.QSuite.Tests.ModbusProtocolTest;

internal static class MasterEngineTests
{
    internal static void Run()
    {
        Tcp_ReadHoldingRegisters_HappyPath().GetAwaiter().GetResult();
        Rtu_ReadInputRegisters_HappyPath().GetAwaiter().GetResult();
        ExceptionResponse_BecomesSlaveException().GetAwaiter().GetResult();
        Timeout_Resends_ThenSucceeds().GetAwaiter().GetResult();
        Timeout_RetriesExhausted_Throws().GetAwaiter().GetResult();
        StaleTransaction_IsIgnored().GetAwaiter().GetResult();
        ForeignUnit_IsIgnored().GetAwaiter().GetResult();
        Writes_UseExpectedFunctions().GetAwaiter().GetResult();
        ConcurrentRequests_AreSerialized().GetAwaiter().GetResult();
    }

    /// <summary>Scripted slave behind a framer: decodes what the engine transmits, lets the test
    /// choose the reply. Master side uses the same framer type as the slave side.</summary>
    private sealed class FakeSlave
    {
        public readonly List<ModbusAdu> Received = [];
        public readonly ModbusMasterEngine Engine;
        public Func<ModbusAdu, byte[]?>? RespondPdu; // null = swallow (timeout)

        private readonly IModbusFramer decodeFramer;
        private readonly IModbusFramer encodeFramer;

        public FakeSlave(bool tcp, int timeoutMs = 100)
        {
            decodeFramer = tcp ? new ModbusTcpFramer() : new ModbusRtuFramer(ModbusFramerRole.Slave);
            encodeFramer = tcp ? new ModbusTcpFramer() : new ModbusRtuFramer(ModbusFramerRole.Slave);
            Engine = new ModbusMasterEngine(tcp ? new ModbusTcpFramer() : new ModbusRtuFramer(ModbusFramerRole.Master))
            {
                TimeoutMs = timeoutMs,
                Transmitter = (frame, _) =>
                {
                    decodeFramer.Append(frame);
                    while (decodeFramer.TryDequeueFrame(out var request))
                    {
                        lock (Received)
                        {
                            Received.Add(request);
                        }

                        var responsePdu = RespondPdu?.Invoke(request);
                        if (responsePdu != null)
                        {
                            Reply(new ModbusAdu(request.UnitId, responsePdu, request.TransactionId));
                        }
                    }

                    return Task.CompletedTask;
                }
            };
        }

        public void Reply(ModbusAdu adu)
        {
            Engine.OnBytesReceived(encodeFramer.Encode(adu));
        }
    }

    private static async Task Tcp_ReadHoldingRegisters_HappyPath()
    {
        var slave = new FakeSlave(tcp: true)
        {
            RespondPdu = request => ModbusPdu.BuildReadRegistersResponse(request.Function, [0x1111, 0x2222])
        };

        var registers = await slave.Engine.ReadRegistersAsync(1, ModbusRegisterType.HoldingRegister, 100, 2);

        Check(registers.SequenceEqual(new ushort[] { 0x1111, 0x2222 }), "TCP read: registers returned");
        Check(slave.Received.Single() is { Function: 0x03, UnitId: 1 }, "TCP read: FC03 sent to unit 1");
    }

    private static async Task Rtu_ReadInputRegisters_HappyPath()
    {
        var slave = new FakeSlave(tcp: false)
        {
            RespondPdu = request => ModbusPdu.BuildReadRegistersResponse(request.Function, [0xABCD])
        };

        var registers = await slave.Engine.ReadRegistersAsync(7, ModbusRegisterType.InputRegister, 5, 1);

        Check(registers.SequenceEqual(new ushort[] { 0xABCD }), "RTU read: register returned through CRC framing");
        Check(slave.Received.Single() is { Function: 0x04, UnitId: 7 }, "RTU read: FC04 sent to unit 7");
    }

    private static async Task ExceptionResponse_BecomesSlaveException()
    {
        var slave = new FakeSlave(tcp: true)
        {
            RespondPdu = request => ModbusPdu.BuildExceptionResponse(request.Function, ModbusExceptionCode.IllegalDataAddress)
        };

        try
        {
            await slave.Engine.ReadRegistersAsync(1, ModbusRegisterType.HoldingRegister, 9999, 1);
            Check(false, "exception response surfaces (none thrown)");
        }
        catch (ModbusSlaveException e)
        {
            Check(e.ExceptionCode == ModbusExceptionCode.IllegalDataAddress, "exception response carries the code");
            Check(e.Message.Contains("ILLEGAL DATA ADDRESS"), "exception message is human-readable");
        }
    }

    private static async Task Timeout_Resends_ThenSucceeds()
    {
        var attempts = 0;
        var slave = new FakeSlave(tcp: true)
        {
            RespondPdu = request => ++attempts == 1
                ? null // swallow the first attempt
                : ModbusPdu.BuildReadRegistersResponse(request.Function, [0x0042])
        };

        var registers = await slave.Engine.ReadRegistersAsync(1, ModbusRegisterType.HoldingRegister, 0, 1);

        Check(registers[0] == 0x0042, "timeout retry: second attempt succeeded");
        Check(attempts == 2, "timeout retry: request was re-sent exactly once");
    }

    private static async Task Timeout_RetriesExhausted_Throws()
    {
        var slave = new FakeSlave(tcp: false) { RespondPdu = _ => null };

        await CheckThrowsAsync<ModbusTimeoutException>(
            () => slave.Engine.ReadRegistersAsync(1, ModbusRegisterType.HoldingRegister, 0, 1),
            "exhausted retries throw ModbusTimeoutException");

        Check(slave.Received.Count == 3, "exhausted retries: initial attempt + 2 retries on the wire");
    }

    private static async Task StaleTransaction_IsIgnored()
    {
        var slave = new FakeSlave(tcp: true, timeoutMs: 300);
        slave.RespondPdu = request =>
        {
            // First reply with a stale transaction id (must be ignored), then the real one.
            slave.Reply(new ModbusAdu(request.UnitId,
                ModbusPdu.BuildReadRegistersResponse(request.Function, [0xDEAD]),
                unchecked((ushort)(request.TransactionId - 1))));
            return ModbusPdu.BuildReadRegistersResponse(request.Function, [0xBEEF]);
        };

        var registers = await slave.Engine.ReadRegistersAsync(1, ModbusRegisterType.HoldingRegister, 0, 1);

        Check(registers[0] == 0xBEEF, "stale TCP transaction id is ignored, matching response wins");
    }

    private static async Task ForeignUnit_IsIgnored()
    {
        var slave = new FakeSlave(tcp: false, timeoutMs: 300);
        slave.RespondPdu = request =>
        {
            slave.Reply(new ModbusAdu(99, ModbusPdu.BuildReadRegistersResponse(request.Function, [0xDEAD])));
            return ModbusPdu.BuildReadRegistersResponse(request.Function, [0xBEEF]);
        };

        var registers = await slave.Engine.ReadRegistersAsync(1, ModbusRegisterType.HoldingRegister, 0, 1);

        Check(registers[0] == 0xBEEF, "RTU response from a foreign unit id is ignored");
    }

    private static async Task Writes_UseExpectedFunctions()
    {
        var slave = new FakeSlave(tcp: true)
        {
            RespondPdu = request => request.Pdu[..5].ToArray() // echo
        };

        await slave.Engine.WriteRegistersAsync(1, 10, [0x1234]);
        await slave.Engine.WriteRegistersAsync(1, 20, [1, 2]);
        await slave.Engine.WriteSingleCoilAsync(1, 30, true);

        Check(slave.Received[0].Function == ModbusFunction.WriteSingleRegister, "single register write uses FC06");
        Check(slave.Received[1].Function == ModbusFunction.WriteMultipleRegisters, "multi register write uses FC16");
        Check(slave.Received[2].Function == ModbusFunction.WriteSingleCoil, "coil write uses FC05");
    }

    private static async Task ConcurrentRequests_AreSerialized()
    {
        var inFlight = 0;
        var overlapped = false;
        var slave = new FakeSlave(tcp: true, timeoutMs: 1000);
        slave.RespondPdu = request =>
        {
            if (Interlocked.Increment(ref inFlight) > 1)
            {
                overlapped = true;
            }

            Thread.Sleep(10);
            Interlocked.Decrement(ref inFlight);
            return ModbusPdu.BuildReadRegistersResponse(request.Function, [0x0001]);
        };

        var reads = Enumerable.Range(0, 5)
            .Select(_ => slave.Engine.ReadRegistersAsync(1, ModbusRegisterType.HoldingRegister, 0, 1));
        await Task.WhenAll(reads);

        Check(!overlapped, "concurrent requests: strictly one request in flight");
        Check(slave.Received.Count == 5, "concurrent requests: all five executed");
    }
}
