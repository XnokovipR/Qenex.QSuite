using System.Buffers.Binary;
using System.Reflection;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;
using ValueDataType = Qenex.QSuite.Variables.QVariables.Values.ValuesGlobal.ValueDataType;

namespace Qenex.QSuite.Protocols.RawCanProtocol;

/// <summary>
/// Simple receive-only protocol over a CAN driver. Each variable is addressed by an 11-bit standard
/// CAN identifier; an incoming frame with a matching id updates that variable's raw value, decoded
/// from the frame bytes according to the variable's type (byte/sbyte, short/ushort, int/uint,
/// long/ulong, float, double), a configurable byte offset and byte order. No J1939 parsing — raw
/// monitoring only. Extended (29-bit) frames are ignored by this protocol.
/// </summary>
public class RawCanProtocol : ProtocolBase<CanFrame>
{
    public RawCanProtocol()
    {
        Specification = new SpecificationBase
        {
            Name = "RawCanProtocol",
            Label = "Raw CAN Protocol",
            Description = "Maps standard (11-bit) CAN frames to variables by id; raw uint32 value, no parsing.",
            CreatedOn = new DateTime(2026, 6, 19),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    public override void SetConfiguration()
    {
    }

    #region Protocol variables

    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, string commParams, bool isCommunicated)
    {
        return new RawCanProtocolVariable
        {
            Variable = variable,
            IsCommunicated = isCommunicated,
            ProtocolVariableSpecification = RawCanProtocolVariableSpecification.Create(commParams),
        };
    }

    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IVarEvent variableEvent, string id)
    {
        return CreateProtocolVariable(variable, $"canId=\"{id}\"", true);
    }

    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IEnumerable<IVarEvent> variableEvents, string commParams, bool isCommunicated)
    {
        return CreateProtocolVariable(variable, commParams, isCommunicated);
    }

    #endregion

    #region Protocol control

    public override Task StartAsync(CancellationToken ct = default)
    {
        SetState(IsEnabled ? CommunicationState.Running : CommunicationState.Disabled);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken ct = default)
    {
        SetState(CommunicationState.Stopped);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
    }

    #endregion

    #region Process received data

    public override async Task AddReceivedDataToQueueAsync(IEnumerable<CanFrame> data, CancellationToken ct = default)
    {
        await ProcessReceivedDataAsync(data, ct);
    }

    protected override void ProcessReceivedData(IEnumerable<CanFrame> data)
    {
        foreach (var protocolVariable in Decode(data))
        {
            protocolVariable.NotifyValueChanged();
        }
    }

    protected override async Task ProcessReceivedDataAsync(IEnumerable<CanFrame> data, CancellationToken ct = default)
    {
        var updated = Decode(data).ToList();
        await Task.WhenAll(updated.Select(protocolVariable => protocolVariable.NotifyValueChangedAsync()));
    }

    #endregion

    #region Encode & Decode

    protected override IEnumerable<CanFrame> Encode(IEnumerable<IProtocolVariable> protocolVariables)
    {
        throw new NotSupportedException("RawCanProtocol is receive-only.");
    }

    protected override IEnumerable<IProtocolVariable> Decode(IEnumerable<CanFrame> data)
    {
        var updated = new List<IProtocolVariable>();

        foreach (var frame in data)
        {
            // This simple protocol handles standard (11-bit) frames only.
            if (frame.IsExtended)
            {
                continue;
            }

            foreach (var protocolVariable in Variables)
            {
                if (!protocolVariable.IsCommunicated ||
                    protocolVariable.ProtocolVariableSpecification is not RawCanProtocolVariableSpecification spec ||
                    spec.CanId != frame.CanId ||
                    protocolVariable.Variable is not ScalarVariable scalarVariable)
                {
                    continue;
                }

                if (TrySetRawValue(scalarVariable, frame, spec))
                {
                    updated.Add(protocolVariable);
                }
            }
        }

        return updated;
    }

    // Builds the variable's raw value from the frame bytes according to the variable's type, the
    // configured offset and byte order. Missing bytes (short frame / offset past the end) are zero-padded.
    private bool TrySetRawValue(ScalarVariable scalarVariable, CanFrame frame, RawCanProtocolVariableSpecification spec)
    {
        var valueType = scalarVariable.Values.ValueType;
        var size = SizeOf(valueType);
        if (size == 0)
        {
            Logger?.Log(LogLevel.Warn,
                $"RawCanProtocol: variable '{scalarVariable.Name}' (id 0x{frame.CanId:X3}) has unsupported type {valueType}.");
            return false;
        }

        // Collect the value's bytes starting at the offset; normalize to little-endian for the readers below.
        Span<byte> buffer = stackalloc byte[8];
        for (var i = 0; i < size; i++)
        {
            var index = spec.Offset + i;
            buffer[i] = index < frame.Data.Length ? frame.Data[index] : (byte)0;
        }

        if (spec.ByteOrder == RawCanByteOrder.BigEndian)
        {
            buffer[..size].Reverse();
        }

        object value = valueType switch
        {
            ValueDataType.Byte => buffer[0],
            ValueDataType.SByte => (sbyte)buffer[0],
            ValueDataType.UShort => BinaryPrimitives.ReadUInt16LittleEndian(buffer),
            ValueDataType.Short => BinaryPrimitives.ReadInt16LittleEndian(buffer),
            ValueDataType.UInt => BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            ValueDataType.Int => BinaryPrimitives.ReadInt32LittleEndian(buffer),
            ValueDataType.ULong => BinaryPrimitives.ReadUInt64LittleEndian(buffer),
            ValueDataType.Long => BinaryPrimitives.ReadInt64LittleEndian(buffer),
            ValueDataType.Float => BinaryPrimitives.ReadSingleLittleEndian(buffer),
            ValueDataType.Double => BinaryPrimitives.ReadDoubleLittleEndian(buffer),
            _ => null!
        };

        scalarVariable.SetValue(value);
        scalarVariable.Timestamp = DateTime.UtcNow;
        return true;
    }

    // Byte width of a scalar value type; 0 for types this protocol cannot map from raw CAN bytes.
    private static int SizeOf(ValueDataType valueType) => valueType switch
    {
        ValueDataType.Byte or ValueDataType.SByte => 1,
        ValueDataType.UShort or ValueDataType.Short => 2,
        ValueDataType.UInt or ValueDataType.Int or ValueDataType.Float => 4,
        ValueDataType.ULong or ValueDataType.Long or ValueDataType.Double => 8,
        _ => 0
    };

    #endregion
}
