using System.Reflection;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.RawCanProtocol;

/// <summary>
/// Simple receive-only protocol over a CAN driver. Each variable is addressed by an 11-bit standard
/// CAN identifier; an incoming frame with a matching id updates that variable's raw value (the first
/// up to 4 data bytes as a little-endian uint32). No J1939 parsing — raw monitoring only.
/// Extended (29-bit) frames are ignored by this protocol.
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
        return CreateProtocolVariable(variable, $"address=\"{id}\"", true);
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

                if (TrySetRawValue(scalarVariable, frame))
                {
                    updated.Add(protocolVariable);
                }
            }
        }

        return updated;
    }

    // Raw value = first up to 4 data bytes as a little-endian uint32 (shorter frames are zero-padded).
    private bool TrySetRawValue(ScalarVariable scalarVariable, CanFrame frame)
    {
        uint value = 0;
        var count = Math.Min(frame.Data.Length, 4);
        for (var i = 0; i < count; i++)
        {
            value |= (uint)frame.Data[i] << (8 * i);
        }

        try
        {
            scalarVariable.SetValue(value);
            scalarVariable.Timestamp = DateTime.UtcNow;
            return true;
        }
        catch (InvalidCastException)
        {
            Logger?.Log(LogLevel.Warn,
                $"RawCanProtocol: variable '{scalarVariable.Name}' (id 0x{frame.CanId:X3}) must be of type UInt (uint32).");
            return false;
        }
    }

    #endregion
}
