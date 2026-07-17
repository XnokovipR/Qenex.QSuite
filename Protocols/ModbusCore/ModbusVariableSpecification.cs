using System.Globalization;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;
using ValueDataType = Qenex.QSuite.Variables.QVariables.Values.ValuesGlobal.ValueDataType;

namespace Qenex.QSuite.Protocols.Modbus;

/// <summary>
/// Per-variable Modbus mapping shared by the master and slave protocols: data table, start address,
/// word order for multi-register values, transfer direction and — for the polling master — the
/// periodic event that drives the poll interval. The value type always comes from the bound
/// variable itself, so decoded values can be assigned directly.
/// </summary>
public class ModbusVariableSpecification : ProtVariableSpecification
{
    public ModbusVariableSpecification()
    {
        Name = "ModbusVariableSpecification";
    }

    /// <summary>Data table the variable lives in.</summary>
    public ModbusRegisterType RegisterType { get; init; } = ModbusRegisterType.HoldingRegister;

    /// <summary>Zero-based start address within the table (bit address for coil/discrete).</summary>
    public ushort Address { get; init; }

    /// <summary>Register order of multi-register values; irrelevant for bits and single registers.</summary>
    public ModbusWordOrder WordOrder { get; init; } = ModbusWordOrder.BigEndian;

    /// <summary>Master: polled read, operator write, or both. Slave ignores this (access is implied
    /// by the data table: coils/holding writable, discrete/input read-only).</summary>
    public CommDirection Direction { get; init; } = CommDirection.Read;

    /// <summary>Event whose period drives the master's polling; null for write-only or slave-side variables.</summary>
    public IVarEvent? VariableEvent { get; init; }

    /// <summary>Value type used for register coding; always the bound variable's own type.
    /// Undefined for matrix variables (transferred as a raw byte block).</summary>
    public ValueDataType DataType { get; init; }

    /// <summary>Raw buffer size of a bound matrix variable in bytes; 0 for scalar variables.</summary>
    public int MatrixByteCount { get; init; }

    /// <summary>Number of 16-bit registers the value occupies (1 for bit tables).</summary>
    public int RegisterCount => IsBitTable
        ? 1
        : MatrixByteCount > 0 ? (MatrixByteCount + 1) / 2 : ModbusRegisterCodec.RegisterCount(DataType);

    public bool IsBitTable => RegisterType is ModbusRegisterType.Coil or ModbusRegisterType.DiscreteInput;

    /// <summary>
    /// Controlled serialization so the configuration round-trips (the XML module handler uses a
    /// property literally named CommParams verbatim). Includes eventRef when an event is bound —
    /// commParams containing "eventRef" are routed to the events-aware CreateProtocolVariable overload.
    /// </summary>
    public string CommParams
    {
        get
        {
            var registerType = RegisterType switch
            {
                ModbusRegisterType.Coil => "coil",
                ModbusRegisterType.DiscreteInput => "discreteInput",
                ModbusRegisterType.InputRegister => "inputRegister",
                _ => "holdingRegister"
            };
            var direction = Direction switch
            {
                CommDirection.Write => "write",
                CommDirection.ReadWrite => "readWrite",
                _ => "read"
            };

            var commParams =
                $"registerType=\"{registerType}\";address=\"{Address}\";" +
                $"wordOrder=\"{(WordOrder == ModbusWordOrder.LittleEndian ? "little" : "big")}\";direction=\"{direction}\"";

            if (VariableEvent != null)
            {
                commParams += $";eventRef=\"{VariableEvent.Name}\"";
            }

            return commParams;
        }
    }

    // commParam example:
    //   registerType="holdingRegister";address="100";wordOrder="big";direction="readWrite";eventRef="poll100ms"
    // Only address is mandatory; registerType defaults to holdingRegister, wordOrder to big,
    // direction to read. requirePollEvent enforces an eventRef for readable master variables.
    public static ModbusVariableSpecification Create(string commParams, IEnumerable<IVarEvent>? variableEvents,
        IVariableBase variable, bool requirePollEvent)
    {
        var settings = Parse(commParams);

        var registerType = ParseRegisterType(settings);
        var address = ParseAddress(settings);
        var wordOrder = ParseWordOrder(settings);
        var direction = ParseDirection(settings);
        var variableEvent = ResolveEvent(settings, variableEvents);
        var matrixByteCount = ResolveMatrixByteCount(variable, registerType, direction);
        var dataType = matrixByteCount > 0 ? ValueDataType.Undefined : ResolveDataType(variable, registerType);

        if (requirePollEvent && variableEvent is null && direction != CommDirection.Write)
        {
            throw new ArgumentException(
                $"Variable '{variable.Name}' has direction '{direction}' but no eventRef; a periodic event is required for polled reads.");
        }

        if (direction != CommDirection.Read &&
            registerType is ModbusRegisterType.DiscreteInput or ModbusRegisterType.InputRegister)
        {
            throw new ArgumentException(
                $"Variable '{variable.Name}': {registerType} is a read-only table and cannot have direction '{direction}'.");
        }

        var registerCount = registerType is ModbusRegisterType.Coil or ModbusRegisterType.DiscreteInput
            ? 1
            : matrixByteCount > 0 ? (matrixByteCount + 1) / 2 : ModbusRegisterCodec.RegisterCount(dataType);
        if (address + registerCount - 1 > ushort.MaxValue)
        {
            throw new ArgumentException($"Variable '{variable.Name}': register range exceeds the 16-bit address space.");
        }

        return new ModbusVariableSpecification
        {
            RegisterType = registerType,
            Address = address,
            WordOrder = wordOrder,
            Direction = direction,
            VariableEvent = variableEvent,
            DataType = dataType,
            MatrixByteCount = matrixByteCount
        };
    }

    #region Parsing

    private static Dictionary<string, string> Parse(string commParams)
    {
        return commParams
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1].Trim('"'), StringComparer.OrdinalIgnoreCase);
    }

    private static ModbusRegisterType ParseRegisterType(IReadOnlyDictionary<string, string> settings)
    {
        if (!settings.TryGetValue("registerType", out var value))
        {
            return ModbusRegisterType.HoldingRegister;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "coil" => ModbusRegisterType.Coil,
            "discreteinput" or "discrete" => ModbusRegisterType.DiscreteInput,
            "inputregister" or "input" => ModbusRegisterType.InputRegister,
            "holdingregister" or "holding" => ModbusRegisterType.HoldingRegister,
            _ => throw new ArgumentException($"Invalid commParam registerType='{value}'.")
        };
    }

    private static ushort ParseAddress(IReadOnlyDictionary<string, string> settings)
    {
        if (!settings.TryGetValue("address", out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Missing mandatory commParam 'address'.");
        }

        var text = value.Trim();
        var isHex = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        if (isHex)
        {
            text = text[2..];
        }

        var parsed = isHex
            ? ushort.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var address)
            : ushort.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out address);

        return parsed ? address : throw new ArgumentException($"Invalid commParam address '{value}'.");
    }

    private static ModbusWordOrder ParseWordOrder(IReadOnlyDictionary<string, string> settings)
    {
        if (!settings.TryGetValue("wordOrder", out var value))
        {
            return ModbusWordOrder.BigEndian;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "big" or "be" => ModbusWordOrder.BigEndian,
            "little" or "le" => ModbusWordOrder.LittleEndian,
            _ => throw new ArgumentException($"Invalid commParam wordOrder='{value}' (expected big/little).")
        };
    }

    private static CommDirection ParseDirection(IReadOnlyDictionary<string, string> settings)
    {
        if (!settings.TryGetValue("direction", out var value))
        {
            return CommDirection.Read;
        }

        return Enum.TryParse<CommDirection>(value, ignoreCase: true, out var direction)
            ? direction
            : throw new ArgumentException($"Invalid commParam direction '{value}'.");
    }

    private static IVarEvent? ResolveEvent(IReadOnlyDictionary<string, string> settings, IEnumerable<IVarEvent>? variableEvents)
    {
        if (!settings.TryGetValue("eventRef", out var eventName) || string.IsNullOrWhiteSpace(eventName))
        {
            return null;
        }

        return variableEvents?.FirstOrDefault(e => e.Name == eventName)
               ?? throw new ArgumentException($"Event '{eventName}' referenced by commParam eventRef was not found.");
    }

    // A matrix variable travels as one raw register block: reads use a single FC 03/04 request
    // (limit 125 registers), element writes a single FC 06/16 request (limit 123 registers), so
    // the whole buffer must fit the stricter of the limits its direction can hit.
    private static int ResolveMatrixByteCount(IVariableBase variable, ModbusRegisterType registerType,
        CommDirection direction)
    {
        if (variable is not MatrixVariable matrixVariable)
        {
            return 0;
        }

        if (registerType is ModbusRegisterType.Coil or ModbusRegisterType.DiscreteInput)
        {
            throw new ArgumentException(
                $"Variable '{variable.Name}': a matrix variable cannot be mapped to the bit table '{registerType}'.");
        }

        var layoutError = matrixVariable.ValidateLayout();
        if (layoutError != null)
        {
            throw new ArgumentException($"Variable '{variable.Name}': invalid matrix layout — {layoutError}");
        }

        var registerCount = (matrixVariable.Size + 1) / 2;
        var limit = direction == CommDirection.Read ? ModbusPdu.MaxRegistersPerRead : ModbusPdu.MaxRegistersPerWrite;
        if (registerCount > limit)
        {
            throw new ArgumentException(
                $"Variable '{variable.Name}': {matrixVariable.Size} bytes need {registerCount} registers, " +
                $"exceeding the Modbus limit of {limit} registers ({limit * 2} bytes) per request.");
        }

        return matrixVariable.Size;
    }

    private static ValueDataType ResolveDataType(IVariableBase variable, ModbusRegisterType registerType)
    {
        if (variable is not ScalarVariable scalarVariable)
        {
            throw new ArgumentException($"Variable '{variable.Name}' is not scalar.");
        }

        var dataType = scalarVariable.Values.ValueType;
        if (ModbusRegisterCodec.RegisterCount(dataType) == 0)
        {
            throw new ArgumentException($"Variable '{variable.Name}' has type '{dataType}' which cannot be mapped to Modbus.");
        }

        return dataType;
    }

    #endregion
}
