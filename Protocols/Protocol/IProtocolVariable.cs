using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Protocols.Protocol;

public interface IProtocolVariable
{
    bool IsCommunicated { get; set; }
    IVariableBase Variable { get; set; }
    IProtVariableSpecification ProtocolVariableSpecification { get; set; }
    
    void NotifyValueChanged();
    event Action? OnValueChanged;
    void SubscribeValueChanged(Action handler);
    void UnsubscribeValueChanged(Action handler);
    
    Task NotifyValueChangedAsync();
    event Func<IProtocolVariable, Task>? OnValueChangedAsync;
    void SubscribeAsyncValueChanged(Func<IProtocolVariable, Task> handler);
    void UnsubscribeAsyncValueChanged(Func<IProtocolVariable, Task> handler);
}