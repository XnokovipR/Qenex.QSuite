using System.Text;
using Qenex.QSuite.Protocols.JsonSignalProtocol;
using Qenex.QSuite.Protocols.PiZeroJsonProtocol;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using ValueDataType = Qenex.QSuite.Variables.QVariables.Values.ValuesGlobal.ValueDataType;

namespace Qenex.QSuite.Tests.JsonProtocolTest;

/// <summary>Console-style unit tests for the JSON line protocols after their migration to raw
/// byte-chunk transports (framing moved from the TCP driver into the protocols). Exit code 0 = all
/// passed.</summary>
internal static class Program
{
    private static int failures;

    private static int Main()
    {
        LineFramer_ReassemblesChunkedLines();
        LineFramer_SplitUtf8Character();
        JsonSignal_ValueFromByteChunks().GetAwaiter().GetResult();
        PiZero_ValueFromByteChunks_And_CommandBytes().GetAwaiter().GetResult();

        Console.WriteLine(failures == 0 ? "ALL TESTS PASSED" : $"{failures} TEST(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    private static void Check(bool condition, string description)
    {
        if (condition)
        {
            return;
        }

        failures++;
        Console.WriteLine($"FAILED: {description}");
    }

    private static void LineFramer_ReassemblesChunkedLines()
    {
        var framer = new TextLineFramer();

        var first = framer.Append("{\"a\":1}\r\n{\"b\""u8);
        Check(first.Count == 1 && first[0] == "{\"a\":1}", "framer: complete CRLF line emitted, partial buffered");

        var second = framer.Append(":2}\n"u8);
        Check(second.Count == 1 && second[0] == "{\"b\":2}", "framer: partial line completed across chunks");

        framer.Reset();
        var afterReset = framer.Append("x}\n"u8);
        Check(afterReset.Count == 1 && afterReset[0] == "x}", "framer: reset drops the partial line");
    }

    private static void LineFramer_SplitUtf8Character()
    {
        var framer = new TextLineFramer();
        var bytes = Encoding.UTF8.GetBytes("motorová řídicí jednotka\n");

        // Feed byte-by-byte so multi-byte UTF-8 sequences are split across chunks.
        var lines = new List<string>();
        foreach (var value in bytes)
        {
            lines.AddRange(framer.Append([value]));
        }

        Check(lines.Count == 1 && lines[0] == "motorová řídicí jednotka",
            "framer: split multi-byte UTF-8 sequences decode correctly");
    }

    private static async Task JsonSignal_ValueFromByteChunks()
    {
        var variable = new ScalarVariable
        {
            Id = 1,
            Name = "EngineTemp",
            Values = new Values<double> { Value = 0, ValueType = ValueDataType.Double }
        };

        var protocol = new JsonSignalProtocol { IsEnabled = true };
        protocol.AddVariable(protocol.CreateProtocolVariable(variable, "name=\"temp\"", true)!);
        await protocol.StartAsync();

        // One JSON line split across two byte chunks, as a TCP driver would deliver it.
        var message = "{\"name\":\"temp\",\"value\":42.5}\n"u8.ToArray();
        await protocol.AddReceivedDataToQueueAsync([message[..10], message[10..]]);

        var deadline = Environment.TickCount64 + 2000;
        while ((double)variable.GetValue() == 0 && Environment.TickCount64 < deadline)
        {
            await Task.Delay(10);
        }

        await protocol.StopAsync();
        Check(Math.Abs((double)variable.GetValue() - 42.5) < 0.0001,
            "JsonSignalProtocol: value decoded from chunked byte stream");
    }

    private static async Task PiZero_ValueFromByteChunks_And_CommandBytes()
    {
        var readVariable = new ScalarVariable
        {
            Id = 1,
            Name = "Speed",
            Values = new Values<double> { Value = 0, ValueType = ValueDataType.Double }
        };
        var writeVariable = new ScalarVariable
        {
            Id = 2,
            Name = "Amplitude",
            Values = new Values<double> { Value = 7.5, ValueType = ValueDataType.Double }
        };

        var protocol = new PiZeroJsonProtocol { IsEnabled = true };
        protocol.AddVariable(protocol.CreateProtocolVariable(readVariable, "direction=\"read\";id=\"speed\";multiplier=\"1\"", true)!);
        protocol.AddVariable(protocol.CreateProtocolVariable(writeVariable, "direction=\"write\";id=\"gen\";param=\"A\";multiplier=\"1\"", true)!);

        var sentChunks = new List<byte[]>();
        protocol.SetTransmitter((bytes, _) =>
        {
            sentChunks.Add(bytes);
            return Task.CompletedTask;
        });

        await protocol.StartAsync();

        // Receive path: chunked JSON line updates the read variable.
        var message = "{\"name\":\"speed\",\"value\":123.5}\n"u8.ToArray();
        await protocol.AddReceivedDataToQueueAsync([message[..7], message[7..]]);
        Check(Math.Abs((double)readVariable.GetValue() - 123.5) < 0.0001,
            "PiZeroJsonProtocol: value decoded from chunked byte stream");

        // Write path: the operator write goes out as one newline-terminated JSON byte line.
        var writeProtocolVariable = protocol.Variables.First(v => v.Variable.Id == 2);
        Check(protocol.CanWriteVariable(writeProtocolVariable), "PiZeroJsonProtocol: write variable reports writable");
        await protocol.WriteVariableAsync(writeProtocolVariable);
        await protocol.StopAsync();

        var sentText = sentChunks.Count == 1 ? Encoding.UTF8.GetString(sentChunks[0]) : string.Empty;
        Check(sentText.EndsWith('\n') && sentText.Contains("\"cmd\":\"set\"") && sentText.Contains("\"name\":\"gen\""),
            "PiZeroJsonProtocol: command serialized as a newline-terminated JSON byte line");
    }
}
