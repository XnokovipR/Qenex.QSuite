using Qenex.QSuite.Protocols.Modbus;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Protocols.ModbusSlave;

/// <summary>
/// The slave's data store backed by protocol variables: each variable claims a range of addresses
/// in its data table (one bit, or RegisterCount consecutive registers). Reads encode the variable's
/// current value on demand; register writes must cover every touched variable completely (a torn
/// half-float write is rejected as ILLEGAL DATA ADDRESS), then decode and assign the new values.
/// </summary>
internal sealed class ModbusVariableRegisterMap : IModbusDataStore
{
    private sealed record RegisterSlot(ModbusProtocolVariable ProtocolVariable, ModbusVariableSpecification Spec, int WordIndex);

    private sealed record BitSlot(ModbusProtocolVariable ProtocolVariable, ModbusVariableSpecification Spec);

    private readonly Dictionary<ushort, RegisterSlot> holdingRegisters = [];
    private readonly Dictionary<ushort, RegisterSlot> inputRegisters = [];
    private readonly Dictionary<ushort, BitSlot> coils = [];
    private readonly Dictionary<ushort, BitSlot> discreteInputs = [];

    /// <summary>Builds the map; throws ArgumentException on overlapping addresses.</summary>
    public static ModbusVariableRegisterMap Build(IEnumerable<IProtocolVariable> variables)
    {
        var map = new ModbusVariableRegisterMap();

        foreach (var protocolVariable in variables)
        {
            if (protocolVariable is not ModbusProtocolVariable modbusVariable ||
                !modbusVariable.IsCommunicated ||
                modbusVariable.ProtocolVariableSpecification is not ModbusVariableSpecification spec ||
                modbusVariable.Variable is not ScalarVariable)
            {
                continue;
            }

            switch (spec.RegisterType)
            {
                case ModbusRegisterType.Coil:
                    map.AddBit(map.coils, modbusVariable, spec);
                    break;
                case ModbusRegisterType.DiscreteInput:
                    map.AddBit(map.discreteInputs, modbusVariable, spec);
                    break;
                case ModbusRegisterType.InputRegister:
                    map.AddRegisters(map.inputRegisters, modbusVariable, spec);
                    break;
                default:
                    map.AddRegisters(map.holdingRegisters, modbusVariable, spec);
                    break;
            }
        }

        return map;
    }

    public bool IsEmpty => holdingRegisters.Count == 0 && inputRegisters.Count == 0 &&
                           coils.Count == 0 && discreteInputs.Count == 0;

    private void AddBit(Dictionary<ushort, BitSlot> table, ModbusProtocolVariable variable, ModbusVariableSpecification spec)
    {
        if (!table.TryAdd(spec.Address, new BitSlot(variable, spec)))
        {
            throw new ArgumentException(
                $"Variable '{variable.Variable.Name}' overlaps address {spec.Address} in the {spec.RegisterType} table.");
        }
    }

    private void AddRegisters(Dictionary<ushort, RegisterSlot> table, ModbusProtocolVariable variable, ModbusVariableSpecification spec)
    {
        for (var word = 0; word < spec.RegisterCount; word++)
        {
            var address = (ushort)(spec.Address + word);
            if (!table.TryAdd(address, new RegisterSlot(variable, spec, word)))
            {
                throw new ArgumentException(
                    $"Variable '{variable.Variable.Name}' overlaps address {address} in the {spec.RegisterType} table.");
            }
        }
    }

    #region IModbusDataStore

    public bool TryReadBits(ModbusRegisterType table, ushort address, int count, out bool[] bits)
    {
        var source = table == ModbusRegisterType.Coil ? coils : discreteInputs;
        bits = new bool[count];

        for (var i = 0; i < count; i++)
        {
            if (!source.TryGetValue((ushort)(address + i), out var slot))
            {
                return false;
            }

            bits[i] = ModbusRegisterCodec.ToBit(slot.ProtocolVariable.Variable.GetValue());
        }

        return true;
    }

    public bool TryReadRegisters(ModbusRegisterType table, ushort address, int count, out ushort[] registers)
    {
        var source = table == ModbusRegisterType.HoldingRegister ? holdingRegisters : inputRegisters;
        registers = new ushort[count];

        // Encode each variable once even when the range covers several of its words.
        var encoded = new Dictionary<ModbusProtocolVariable, ushort[]>();
        for (var i = 0; i < count; i++)
        {
            if (!source.TryGetValue((ushort)(address + i), out var slot))
            {
                return false;
            }

            if (!encoded.TryGetValue(slot.ProtocolVariable, out var words))
            {
                words = ModbusRegisterCodec.EncodeValue(slot.ProtocolVariable.Variable.GetValue(),
                    slot.Spec.DataType, slot.Spec.WordOrder);
                encoded[slot.ProtocolVariable] = words;
            }

            registers[i] = words[slot.WordIndex];
        }

        return true;
    }

    public async ValueTask<bool> TryWriteBitAsync(ushort address, bool value, CancellationToken ct = default)
    {
        if (!coils.TryGetValue(address, out var slot))
        {
            return false;
        }

        await ApplyValueAsync(slot.ProtocolVariable, ModbusRegisterCodec.FromBit(value, slot.Spec.DataType));
        return true;
    }

    public async ValueTask<bool> TryWriteRegistersAsync(ushort address, ushort[] values, CancellationToken ct = default)
    {
        // Collect the touched variables and verify the write covers each of them completely.
        var touched = new Dictionary<ModbusProtocolVariable, ModbusVariableSpecification>();
        for (var i = 0; i < values.Length; i++)
        {
            if (!holdingRegisters.TryGetValue((ushort)(address + i), out var slot))
            {
                return false;
            }

            touched.TryAdd(slot.ProtocolVariable, slot.Spec);
        }

        foreach (var (_, spec) in touched)
        {
            if (spec.Address < address || spec.Address + spec.RegisterCount > address + values.Length)
            {
                return false; // partial (torn) write of a multi-register value
            }
        }

        foreach (var (protocolVariable, spec) in touched)
        {
            var words = values.AsSpan(spec.Address - address, spec.RegisterCount);
            var value = ModbusRegisterCodec.DecodeValue(words, spec.DataType, spec.WordOrder);
            await ApplyValueAsync(protocolVariable, value);
        }

        return true;
    }

    #endregion

    private static async Task ApplyValueAsync(ModbusProtocolVariable protocolVariable, object value)
    {
        var scalarVariable = (ScalarVariable)protocolVariable.Variable;
        scalarVariable.SetValue(value);
        scalarVariable.Timestamp = DateTime.UtcNow;

        // Flag the update as bus-originated so a shared operator-write wiring cannot echo it back.
        protocolVariable.IsUpdatingFromBus = true;
        try
        {
            await protocolVariable.NotifyValueChangedAsync();
        }
        finally
        {
            protocolVariable.IsUpdatingFromBus = false;
        }
    }
}
