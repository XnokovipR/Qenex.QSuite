using System.Buffers.Binary;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Protocols.XcpCore;
using Qenex.QSuite.Protocols.XcpTcpProtocol;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.Variables.VariableEvents;
using ValueDataType = Qenex.QSuite.Variables.QVariables.Values.ValuesGlobal.ValueDataType;
using static Qenex.QSuite.Tests.XcpProtocolTest.Program;

namespace Qenex.QSuite.Tests.XcpProtocolTest;

/// <summary>
/// End-to-end DAQ over the XcpTcp protocol against a simulated XCP-on-Ethernet slave (the XCPlite
/// shape: ODT+FIL+DAQW identification, fixed 32-bit timestamps): setup sequence, DTO decoding into
/// variables (D5 stage 4a: PC receive time), polling coexistence (D4) and the no-DAQ-resource
/// fallback to polling.
/// </summary>
internal static class TcpDaqIntegrationTests
{
    internal static void Run()
    {
        DaqFlow_DtoLandsInVariable_PollingCoexists().GetAwaiter().GetResult();
        NoDaqResource_FallsBackToPolling().GetAwaiter().GetResult();
    }

    #region Simulated DAQ slave harness

    private sealed class SimulatedDaqSlave
    {
        private readonly XcpTcp protocol;
        private readonly List<byte> rxStream = [];

        public readonly List<byte[]> SentPackets = [];
        public byte ConnectResource = 0x05; // CAL + DAQ
        public byte[] PolledMemory = new byte[8];

        public SimulatedDaqSlave(XcpTcp xcpTcpProtocol)
        {
            protocol = xcpTcpProtocol;
            protocol.SetTransmitter(async (chunk, ct) =>
            {
                List<byte[]> commands = [];
                lock (SentPackets)
                {
                    rxStream.AddRange(chunk);
                    while (rxStream.Count >= 4)
                    {
                        var length = rxStream[0] | (rxStream[1] << 8);
                        if (rxStream.Count < 4 + length)
                        {
                            break;
                        }

                        var command = rxStream.GetRange(4, length).ToArray();
                        rxStream.RemoveRange(0, 4 + length);
                        SentPackets.Add(command);
                        commands.Add(command);
                    }
                }

                foreach (var command in commands)
                {
                    var response = Respond(command);
                    if (response != null)
                    {
                        await SendToMasterAsync(response, ct);
                    }
                }
            });
        }

        public int CountSent(byte pid)
        {
            lock (SentPackets)
            {
                return SentPackets.Count(p => p[0] == pid);
            }
        }

        public byte[]? FindSent(byte pid)
        {
            lock (SentPackets)
            {
                return SentPackets.FirstOrDefault(p => p[0] == pid);
            }
        }

        /// <summary>Frames one slave packet (RES/EV/DTO) and loops it back into the protocol.</summary>
        public async Task SendToMasterAsync(byte[] packet, CancellationToken ct = default)
        {
            var frame = new byte[4 + packet.Length];
            BinaryPrimitives.WriteUInt16LittleEndian(frame, (ushort)packet.Length);
            packet.CopyTo(frame, 4);
            await protocol.AddReceivedDataToQueueAsync([frame], ct);
        }

        private byte[]? Respond(byte[] command) => command[0] switch
        {
            XcpCommand.Connect => [0xFF, ConnectResource, 0x00, 0xFA, 0xFF, 0x05, 0x01, 0x01],
            XcpCommand.GetStatus => [0xFF, 0x00, 0x00, 0x00, 0x00, 0x00],
            XcpCommand.Disconnect => [0xFF],
            XcpCommand.Synch => [0xFE, XcpErrorCode.CmdSynch],
            XcpCommand.ShortUpload => [(byte)0xFF, .. PolledMemory.Take(command[1])],
            XcpCommand.Upload => [0xFF, (byte)'l', (byte)'o', (byte)'o', (byte)'p'],
            // XCPlite shape: dynamic lists, timestamps, overload via PID, ODT+FIL+DAQW, ext per DAQ.
            XcpCommand.GetDaqProcessorInfo => [0xFF, 0x51, 0x00, 0x00, 0x02, 0x00, 0x00, 0xF0],
            // Fixed 4-byte timestamps (unit 1 ms, 1 tick); max ODT entry 250 bytes.
            XcpCommand.GetDaqResolutionInfo => [0xFF, 0x01, 0xFA, 0x01, 0xFA, 0x6C, 0x01, 0x00],
            // DAQ-capable channel, 4-char name ("loop" via UPLOAD), nominal cycle 1 ms.
            XcpCommand.GetDaqEventInfo => [0xFF, 0x84, 0xFF, 0x04, 0x01, 0x06, 0x00],
            XcpCommand.FreeDaq or XcpCommand.AllocDaq or XcpCommand.AllocOdt or XcpCommand.AllocOdtEntry
                or XcpCommand.SetDaqPtr or XcpCommand.WriteDaq or XcpCommand.SetDaqListMode
                or XcpCommand.StartStopSynch => [0xFF],
            XcpCommand.StartStopDaqList => [0xFF, 0x00],
            _ => null
        };
    }

    private static ScalarVariable DoubleVariable(int id, string name) => new()
    {
        Id = id,
        Name = name,
        Values = new Values<double> { Value = 0d, ValueType = ValueDataType.Double }
    };

    private static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(10);
        }

        return condition();
    }

    private static (XcpTcp Protocol, SimulatedDaqSlave Slave, ScalarVariable DaqVariable, ScalarVariable PolledVariable)
        CreateRunningSetup()
    {
        var protocol = new XcpTcp
        {
            IsEnabled = true,
            RawSettings = "timeoutMs=\"100\""
        };
        protocol.SetConfiguration();

        var daqEvent = new PeriodicVarEvent
        {
            Name = "daq1ms", Period = 1, Unit = TimeUnit.Milisec,
            EventExtraParams = "direction=\"DAQ\";daqId=\"1\""
        };
        var pollEvent = new PeriodicVarEvent { Name = "poll20ms", Period = 20, Unit = TimeUnit.Milisec };
        IVarEvent[] events = [daqEvent, pollEvent];

        var daqVariable = DoubleVariable(1, "HeatEnergy");
        var polledVariable = DoubleVariable(2, "FlowRate");
        protocol.AddVariable(protocol.CreateProtocolVariable(daqVariable, events,
            "address=\"0x1000\";direction=\"read\";eventRef=\"daq1ms\"", true)!);
        protocol.AddVariable(protocol.CreateProtocolVariable(polledVariable, events,
            "address=\"0x2000\";direction=\"read\";eventRef=\"poll20ms\"", true)!);

        var slave = new SimulatedDaqSlave(protocol);
        return (protocol, slave, daqVariable, polledVariable);
    }

    #endregion

    private static async Task DaqFlow_DtoLandsInVariable_PollingCoexists()
    {
        var (protocol, slave, daqVariable, polledVariable) = CreateRunningSetup();
        BinaryPrimitives.WriteDoubleLittleEndian(slave.PolledMemory, 12.5);

        await protocol.StartAsync();
        var daqStarted = await WaitUntilAsync(() => slave.CountSent(XcpCommand.StartStopSynch) >= 1);
        Check(daqStarted, "tcp daq: configuration ran through to START_STOP_SYNCH");

        // One DTO from the slave: ODT 0 of DAQ list 0 (header ODT|FIL|DAQW), fixed 32-bit
        // timestamp, then the 8-byte double.
        var dto = new byte[4 + 4 + 8];
        dto[0] = 0x00;
        dto[1] = 0xAA;
        BinaryPrimitives.WriteUInt16LittleEndian(dto.AsSpan(2), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(dto.AsSpan(4), 123456);
        BinaryPrimitives.WriteDoubleLittleEndian(dto.AsSpan(8), 7.75);
        await slave.SendToMasterAsync(dto);

        var daqLanded = await WaitUntilAsync(() => (double)daqVariable.GetValue() == 7.75);
        var pollLanded = await WaitUntilAsync(() => (double)polledVariable.GetValue() == 12.5);
        await protocol.StopAsync();

        Check(daqLanded, "tcp daq: DTO value landed in the DAQ variable (timestamp = receive time, 4a)");
        Check(pollLanded, "tcp daq: the polled variable still updates via SHORT_UPLOAD (D4 coexistence)");

        var setMode = slave.FindSent(XcpCommand.SetDaqListMode);
        Check(setMode != null && (setMode[1] & 0x10) != 0,
            "tcp daq: SET_DAQ_LIST_MODE carries the timestamp bit (fixed timestamps)");
        Check(setMode != null && BinaryPrimitives.ReadUInt16LittleEndian(setMode.AsSpan(4)) == 1,
            "tcp daq: DAQ list bound to ECU event channel 1 (daqId from eventExtraParams)");

        var writeDaq = slave.FindSent(XcpCommand.WriteDaq);
        Check(writeDaq != null && writeDaq[2] == 8 &&
              BinaryPrimitives.ReadUInt32LittleEndian(writeDaq.AsSpan(4)) == 0x1000,
            "tcp daq: WRITE_DAQ carries the variable's size and address");

        lock (slave.SentPackets)
        {
            Check(slave.SentPackets.Where(p => p[0] == XcpCommand.ShortUpload)
                    .All(p => BinaryPrimitives.ReadUInt32LittleEndian(p.AsSpan(4)) == 0x2000),
                "tcp daq: the DAQ variable is never polled — SHORT_UPLOAD only reads the polled one");
        }
    }

    private static async Task NoDaqResource_FallsBackToPolling()
    {
        var (protocol, slave, daqVariable, _) = CreateRunningSetup();
        slave.ConnectResource = 0x01; // CAL only, no DAQ
        BinaryPrimitives.WriteDoubleLittleEndian(slave.PolledMemory, 3.5);

        await protocol.StartAsync();
        var landed = await WaitUntilAsync(() => (double)daqVariable.GetValue() == 3.5);
        await protocol.StopAsync();

        Check(landed, "tcp daq fallback: without a DAQ resource the DAQ variable is polled (D4)");
        Check(slave.CountSent(XcpCommand.FreeDaq) == 0, "tcp daq fallback: no DAQ configuration was attempted");
        lock (slave.SentPackets)
        {
            Check(slave.SentPackets.Any(p =>
                    p[0] == XcpCommand.ShortUpload && BinaryPrimitives.ReadUInt32LittleEndian(p.AsSpan(4)) == 0x1000),
                "tcp daq fallback: SHORT_UPLOAD reads the DAQ variable's address");
        }
    }
}
