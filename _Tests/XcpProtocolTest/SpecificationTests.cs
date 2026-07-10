using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Protocols.XcpProtocol;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.Variables.VariableEvents;
using ValueDataType = Qenex.QSuite.Variables.QVariables.Values.ValuesGlobal.ValueDataType;
using static Qenex.QSuite.Tests.XcpProtocolTest.Program;

namespace Qenex.QSuite.Tests.XcpProtocolTest;

internal static class SpecificationTests
{
    private static readonly IVarEvent Poll100Ms = new PeriodicVarEvent { Name = "poll100ms", Period = 100, Unit = TimeUnit.Milisec };

    internal static void Run()
    {
        FullCommParams_ParseAllFields();
        HexAndDecimalAddress_BothParse();
        Defaults_DataTypeAndSizeFromVariable();
        CommParams_RoundTrip();
        WriteOnly_NoEventRef_IsValid();
        ReadWithoutEvent_Throws();
        MissingAddress_Throws();
        DataTypeMismatch_Throws();
        SizeMismatch_Throws();
        UnknownEventRef_Throws();
        XcpProtocol_BadCommParams_ReturnsNull();
        XcpProtocol_GoodCommParams_CreatesVariable();
    }

    private static ScalarVariable FloatVariable(string name = "EngineTemp") => new()
    {
        Id = 1,
        Name = name,
        Values = new Values<float> { Value = 0f, ValueType = ValueDataType.Float }
    };

    private static void FullCommParams_ParseAllFields()
    {
        var spec = XcpVariableSpecification.Create(
            "address=\"0x1A0000\";addressExtension=\"2\";size=\"4\";dataType=\"Float\";direction=\"readWrite\";multiplier=\"3\";eventRef=\"poll100ms\"",
            [Poll100Ms], FloatVariable());

        Check(spec.Address == 0x1A0000, "full commParams: hex address parsed");
        Check(spec.AddressExtension == 2, "full commParams: address extension parsed");
        Check(spec.Size == 4, "full commParams: size parsed");
        Check(spec.DataType == ValueDataType.Float, "full commParams: data type parsed");
        Check(spec.Direction == CommDirection.ReadWrite, "full commParams: direction parsed");
        Check(spec.Multiplier == 3, "full commParams: multiplier parsed");
        Check(ReferenceEquals(spec.VariableEvent, Poll100Ms), "full commParams: event resolved by name");
    }

    private static void HexAndDecimalAddress_BothParse()
    {
        var hex = XcpVariableSpecification.Create("address=\"0xFF\";eventRef=\"poll100ms\"", [Poll100Ms], FloatVariable());
        var dec = XcpVariableSpecification.Create("address=\"255\";eventRef=\"poll100ms\"", [Poll100Ms], FloatVariable());

        Check(hex.Address == 255, "0x-prefixed address is parsed as hex");
        Check(dec.Address == 255, "plain address is parsed as decimal");
    }

    private static void Defaults_DataTypeAndSizeFromVariable()
    {
        var spec = XcpVariableSpecification.Create("address=\"0x10\";eventRef=\"poll100ms\"", [Poll100Ms], FloatVariable());

        Check(spec.DataType == ValueDataType.Float, "missing dataType defaults to the variable's own type");
        Check(spec.Size == 4, "missing size defaults to the type width");
        Check(spec.Direction == CommDirection.Read, "missing direction defaults to read");
        Check(spec.Multiplier == 1, "missing multiplier defaults to 1");
        Check(spec.AddressExtension == 0, "missing addressExtension defaults to 0");
    }

    private static void CommParams_RoundTrip()
    {
        var original = XcpVariableSpecification.Create(
            "address=\"0x1A0000\";addressExtension=\"1\";dataType=\"Float\";direction=\"readWrite\";eventRef=\"poll100ms\"",
            [Poll100Ms], FloatVariable());

        var reparsed = XcpVariableSpecification.Create(original.CommParams, [Poll100Ms], FloatVariable());

        Check(original.CommParams.Contains("eventRef=\"poll100ms\""),
            "serialized commParams contain eventRef (routes XML loading to the events overload)");
        Check(reparsed.Address == original.Address, "round-trip preserves address");
        Check(reparsed.AddressExtension == original.AddressExtension, "round-trip preserves address extension");
        Check(reparsed.Size == original.Size, "round-trip preserves size");
        Check(reparsed.DataType == original.DataType, "round-trip preserves data type");
        Check(reparsed.Direction == original.Direction, "round-trip preserves direction");
        Check(reparsed.Multiplier == original.Multiplier, "round-trip preserves multiplier");
        Check(ReferenceEquals(reparsed.VariableEvent, Poll100Ms), "round-trip preserves event binding");
    }

    private static void WriteOnly_NoEventRef_IsValid()
    {
        var spec = XcpVariableSpecification.Create("address=\"0x20\";direction=\"write\"", null, FloatVariable());

        Check(spec.Direction == CommDirection.Write, "write-only variable parses without eventRef");
        Check(spec.VariableEvent == null, "write-only variable has no event");
        Check(!spec.CommParams.Contains("eventRef"), "write-only serialization omits eventRef");
    }

    private static void ReadWithoutEvent_Throws()
    {
        CheckThrows<ArgumentException>(
            () => XcpVariableSpecification.Create("address=\"0x20\"", null, FloatVariable()),
            "read direction without eventRef is rejected");
    }

    private static void MissingAddress_Throws()
    {
        CheckThrows<ArgumentException>(
            () => XcpVariableSpecification.Create("eventRef=\"poll100ms\"", [Poll100Ms], FloatVariable()),
            "missing address is rejected");
    }

    private static void DataTypeMismatch_Throws()
    {
        CheckThrows<ArgumentException>(
            () => XcpVariableSpecification.Create(
                "address=\"0x10\";dataType=\"Int\";eventRef=\"poll100ms\"", [Poll100Ms], FloatVariable()),
            "dataType differing from the variable's own type is rejected");
    }

    private static void SizeMismatch_Throws()
    {
        CheckThrows<ArgumentException>(
            () => XcpVariableSpecification.Create(
                "address=\"0x10\";size=\"2\";eventRef=\"poll100ms\"", [Poll100Ms], FloatVariable()),
            "size differing from the type width is rejected");
    }

    private static void UnknownEventRef_Throws()
    {
        CheckThrows<ArgumentException>(
            () => XcpVariableSpecification.Create("address=\"0x10\";eventRef=\"nonsense\"", [Poll100Ms], FloatVariable()),
            "unknown eventRef is rejected");
    }

    private static void XcpProtocol_BadCommParams_ReturnsNull()
    {
        var protocol = new Xcp();
        var variable = protocol.CreateProtocolVariable(FloatVariable(), [Poll100Ms], "address=\"bogus\"", true);

        Check(variable == null, "protocol returns null (and logs) for invalid commParams");
    }

    private static void XcpProtocol_GoodCommParams_CreatesVariable()
    {
        var protocol = new Xcp();
        var variable = protocol.CreateProtocolVariable(FloatVariable(), [Poll100Ms],
            "address=\"0x1000\";direction=\"readWrite\";eventRef=\"poll100ms\"", true);

        Check(variable is XcpProtocolVariable, "protocol creates an XcpProtocolVariable");
        Check(variable?.ProtocolVariableSpecification is XcpVariableSpecification { Address: 0x1000 },
            "created variable carries the parsed specification");
    }
}
