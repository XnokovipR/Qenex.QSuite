using Qenex.QSuite.Protocols.Modbus;
using static Qenex.QSuite.Tests.ModbusProtocolTest.Program;

namespace Qenex.QSuite.Tests.ModbusProtocolTest;

internal static class SlaveEngineTests
{
    internal static void Run()
    {
        ReadRegisters_ServedFromStore().GetAwaiter().GetResult();
        ReadBits_ServedFromStore().GetAwaiter().GetResult();
        UnmappedAddress_IllegalDataAddress().GetAwaiter().GetResult();
        UnknownFunction_IllegalFunction().GetAwaiter().GetResult();
        WriteSingleRegister_ReachesStore().GetAwaiter().GetResult();
        WriteMultipleRegisters_ReachesStore().GetAwaiter().GetResult();
        WriteCoil_ValidatesValueField().GetAwaiter().GetResult();
        ForeignUnit_Ignored_UnlessAnyUnitMode().GetAwaiter().GetResult();
    }

    private sealed class DictionaryStore : IModbusDataStore
    {
        public readonly Dictionary<ushort, ushort> Holding = [];
        public readonly Dictionary<ushort, ushort> Input = [];
        public readonly Dictionary<ushort, bool> Coils = [];
        public readonly Dictionary<ushort, bool> Discrete = [];

        public bool TryReadBits(ModbusRegisterType table, ushort address, int count, out bool[] bits)
        {
            var source = table == ModbusRegisterType.Coil ? Coils : Discrete;
            bits = new bool[count];
            for (var i = 0; i < count; i++)
            {
                if (!source.TryGetValue((ushort)(address + i), out bits[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryReadRegisters(ModbusRegisterType table, ushort address, int count, out ushort[] registers)
        {
            var source = table == ModbusRegisterType.HoldingRegister ? Holding : Input;
            registers = new ushort[count];
            for (var i = 0; i < count; i++)
            {
                if (!source.TryGetValue((ushort)(address + i), out registers[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public ValueTask<bool> TryWriteBitAsync(ushort address, bool value, CancellationToken ct = default)
        {
            if (!Coils.ContainsKey(address))
            {
                return ValueTask.FromResult(false);
            }

            Coils[address] = value;
            return ValueTask.FromResult(true);
        }

        public ValueTask<bool> TryWriteRegistersAsync(ushort address, ushort[] values, CancellationToken ct = default)
        {
            for (var i = 0; i < values.Length; i++)
            {
                if (!Holding.ContainsKey((ushort)(address + i)))
                {
                    return ValueTask.FromResult(false);
                }
            }

            for (var i = 0; i < values.Length; i++)
            {
                Holding[(ushort)(address + i)] = values[i];
            }

            return ValueTask.FromResult(true);
        }
    }

    private static (ModbusSlaveEngine Engine, DictionaryStore Store) CreateEngine(byte unitId = 1)
    {
        var store = new DictionaryStore();
        return (new ModbusSlaveEngine(store) { UnitId = unitId }, store);
    }

    private static async Task ReadRegisters_ServedFromStore()
    {
        var (engine, store) = CreateEngine();
        store.Holding[10] = 0x1111;
        store.Holding[11] = 0x2222;

        var response = await engine.ProcessRequestAsync(new ModbusAdu(1, ModbusPdu.BuildReadRequest(0x03, 10, 2), 5));

        Check(response is { TransactionId: 5, UnitId: 1 }, "slave read: response echoes unit and transaction");
        Check(ModbusPdu.ParseReadRegistersResponse(response!.Pdu, 2).SequenceEqual(new ushort[] { 0x1111, 0x2222 }),
            "slave read: holding registers served from the store");
    }

    private static async Task ReadBits_ServedFromStore()
    {
        var (engine, store) = CreateEngine();
        store.Discrete[0] = true;
        store.Discrete[1] = false;
        store.Discrete[2] = true;

        var response = await engine.ProcessRequestAsync(new ModbusAdu(1, ModbusPdu.BuildReadRequest(0x02, 0, 3)));

        Check(ModbusPdu.ParseReadBitsResponse(response!.Pdu, 3).SequenceEqual(new[] { true, false, true }),
            "slave read: discrete inputs served from the store");
    }

    private static async Task UnmappedAddress_IllegalDataAddress()
    {
        var (engine, _) = CreateEngine();

        var response = await engine.ProcessRequestAsync(new ModbusAdu(1, ModbusPdu.BuildReadRequest(0x03, 1000, 1)));

        Check(response is { IsException: true, ExceptionCode: ModbusExceptionCode.IllegalDataAddress },
            "unmapped read -> ILLEGAL DATA ADDRESS");
    }

    private static async Task UnknownFunction_IllegalFunction()
    {
        var (engine, _) = CreateEngine();

        var response = await engine.ProcessRequestAsync(new ModbusAdu(1, [0x2B, 0x00, 0x00, 0x00, 0x00]));

        Check(response is { IsException: true, ExceptionCode: ModbusExceptionCode.IllegalFunction },
            "unsupported function -> ILLEGAL FUNCTION");
    }

    private static async Task WriteSingleRegister_ReachesStore()
    {
        var (engine, store) = CreateEngine();
        store.Holding[5] = 0;

        var response = await engine.ProcessRequestAsync(new ModbusAdu(1, ModbusPdu.BuildWriteSingleRegister(5, 0xCAFE)));

        Check(response is { IsException: false }, "FC06 write: positive echo");
        Check(store.Holding[5] == 0xCAFE, "FC06 write: value stored");
    }

    private static async Task WriteMultipleRegisters_ReachesStore()
    {
        var (engine, store) = CreateEngine();
        store.Holding[20] = 0;
        store.Holding[21] = 0;

        var request = ModbusPdu.BuildWriteMultipleRegisters(20, [0x0102, 0x0304]);
        var response = await engine.ProcessRequestAsync(new ModbusAdu(1, request));

        Check(response is { IsException: false }, "FC16 write: positive echo");
        Check(store.Holding[20] == 0x0102 && store.Holding[21] == 0x0304, "FC16 write: values stored");

        var partiallyUnmapped = ModbusPdu.BuildWriteMultipleRegisters(21, [1, 2]);
        var rejected = await engine.ProcessRequestAsync(new ModbusAdu(1, partiallyUnmapped));
        Check(rejected is { IsException: true, ExceptionCode: ModbusExceptionCode.IllegalDataAddress },
            "FC16 write spanning unmapped registers is rejected atomically");
    }

    private static async Task WriteCoil_ValidatesValueField()
    {
        var (engine, store) = CreateEngine();
        store.Coils[3] = false;

        var on = await engine.ProcessRequestAsync(new ModbusAdu(1, ModbusPdu.BuildWriteSingleCoil(3, true)));
        Check(on is { IsException: false } && store.Coils[3], "FC05 write: coil switched on");

        // Illegal value field (anything but 0x0000/0xFF00).
        var malformed = await engine.ProcessRequestAsync(new ModbusAdu(1, [0x05, 0x00, 0x03, 0x12, 0x34]));
        Check(malformed is { IsException: true, ExceptionCode: ModbusExceptionCode.IllegalDataValue },
            "FC05 write with illegal value field -> ILLEGAL DATA VALUE");
    }

    private static async Task ForeignUnit_Ignored_UnlessAnyUnitMode()
    {
        var (engine, store) = CreateEngine(unitId: 1);
        store.Holding[0] = 7;

        var foreign = await engine.ProcessRequestAsync(new ModbusAdu(9, ModbusPdu.BuildReadRequest(0x03, 0, 1)));
        Check(foreign == null, "request for another unit id is ignored silently");

        engine.RespondToAnyUnit = true;
        var accepted = await engine.ProcessRequestAsync(new ModbusAdu(9, ModbusPdu.BuildReadRequest(0x03, 0, 1)));
        Check(accepted is { UnitId: 9, IsException: false }, "any-unit mode answers and echoes the unit id");
    }
}
