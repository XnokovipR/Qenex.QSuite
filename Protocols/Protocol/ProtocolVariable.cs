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
        var handlers = OnValueChanged?.GetInvocationList();
        if (handlers == null)
        {
            return;
        }

        foreach (Action handler in handlers)
        {
            try
            {
                handler();
            }
            catch
            {
            }
        }
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
    
    public async Task NotifyValueChangedAsync()
    {
        var handlers = OnValueChangedAsync?.GetInvocationList();
        if (handlers == null)
        {
            return;
        }

        var tasks = handlers
            .Cast<Func<IProtocolVariable, Task>>()
            .Select(NotifySubscriberAsync)
            .ToList();

        await Task.WhenAll(tasks);
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

    private async Task NotifySubscriberAsync(Func<IProtocolVariable, Task> handler)
    {
        try
        {
            await handler(this);
        }
        catch
        {
        }
    }
}
