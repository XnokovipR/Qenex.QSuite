using System.Reflection;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.PassThroughProtocol;

public class PassThroughProtocol : ProtocolBase<IProtocolVariable>, IProtocolVariableSinkProtocol
{
    public PassThroughProtocol()
    {
        Specification = new SpecificationBase
        {
            Name = "PassThroughProtocol",
            Label = "Data Log Pass-Through",
            Description = "Passes observed variable values 1:1 to the Data Log Recorder driver.",
            CreatedOn = new DateTime(2026, 5, 25),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    public override void SetConfiguration()
    {
    }

    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, string commParams, bool isCommunicated)
    {
        return new PassThroughProtocolVariable
        {
            Variable = variable,
            IsCommunicated = isCommunicated,
            ProtocolVariableSpecification = PassThroughProtocolVariableSpecification.Create(commParams)
        };
    }

    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IVarEvent variableEvent, string id)
    {
        return CreateProtocolVariable(variable, $"id=\"{id}\"", true);
    }

    public override IProtocolVariable? CreateProtocolVariable(
        IVariableBase variable,
        IEnumerable<IVarEvent> variableEvents,
        string commParams,
        bool isCommunicated)
    {
        return CreateProtocolVariable(variable, commParams, isCommunicated);
    }

    public bool CanProcess(IProtocolVariable sourceVariable)
    {
        return Variables.Any(variable => variable.Variable.Id == sourceVariable.Variable.Id);
    }

    public ValueTask<IProtocolVariable?> ProcessObservedValueAsync(
        IProtocolVariable sourceVariable,
        CancellationToken ct = default)
    {
        return ValueTask.FromResult(CanProcess(sourceVariable) ? sourceVariable : null);
    }

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

    public override Task AddReceivedDataToQueueAsync(IEnumerable<IProtocolVariable> data, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    protected override void ProcessReceivedData(IEnumerable<IProtocolVariable> data)
    {
    }

    protected override Task ProcessReceivedDataAsync(IEnumerable<IProtocolVariable> data, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    protected override IEnumerable<IProtocolVariable> Encode(IEnumerable<IProtocolVariable> protocolVariables)
    {
        return protocolVariables;
    }

    protected override IEnumerable<IProtocolVariable> Decode(IEnumerable<IProtocolVariable> data)
    {
        return data;
    }
}
