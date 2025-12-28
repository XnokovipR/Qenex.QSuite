using Qenex.QSuite.Specifications.ComponentSpecification;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Protocols.Protocol;

public class ProtocolVariable : IProtocolVariable
{
    public bool IsCommunicated { get; set; }
    public IVariableBase Variable { get; set; }
    public IProtVariableSpecification ProtocolVariableSpecification { get; set; }

    public void NotifyValueChanged()
    {
        OnValueChanged?.Invoke();
    }
    public event Action? OnValueChanged;
    public void SubscribeValueChanged(Action handler)
    {
        OnValueChanged += handler;
    }
    public void UnsubscribeValueChanged(Action handler)
    {
        OnValueChanged -= handler;
    }
    
    public Task NotifyValueChangedAsync()
    {
        return OnValueChangedAsync?.Invoke(this) ?? Task.CompletedTask;
    }
    public event Func<IProtocolVariable, Task>? OnValueChangedAsync;
    public void SubscribeAsyncValueChanged(Func<IProtocolVariable, Task> handler)
    {
        OnValueChangedAsync += handler;
    }
    public void UnsubscribeAsyncValueChanged(Func<IProtocolVariable, Task> handler)
    {
        OnValueChangedAsync -= handler;
    }
}