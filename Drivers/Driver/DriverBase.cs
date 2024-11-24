using System.ComponentModel;
using System.Text.Json.Serialization;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Driver;

public abstract class DriverBase : IDriverBase
{
    protected bool ExitRequested { get; set; } = false;
    
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }
    
    [JsonIgnore]
    public bool IsStarted { get; protected set; } = false;

    [JsonPropertyName("specification")]
    public ISpecification Specification { get; init; } = null!;
    
    [JsonPropertyName("protocols")]
    public IList<IProtocolBase> Protocols { get; init; } = new List<IProtocolBase>();

    public abstract Task StartAsync(CancellationToken ct = default);
    public abstract Task StopAsync(CancellationToken ct = default);
    public abstract void Dispose();
    
    public virtual void AddProtocol(IProtocolBase protocol)
    {
        Protocols.Add(protocol);
    }

    public virtual void RemoveProtocol(IProtocolBase protocol)
    {
        Protocols.Remove(protocol);
    }

    public virtual void RemoveProtocol(string protocolName)
    {
        Protocols.Remove(Protocols.FirstOrDefault(p => p.Specification.Name == protocolName) ??
                         throw new InvalidEnumArgumentException("Protocol not found"));
    }

    public abstract void Send<T>(T data);

    public abstract Task SendAsync<T>(T data, CancellationToken ct = default);

    public event EventHandler? OnDataReceived;
    
    protected void RaiseOnDataReceive<T>(T data)
    {
        var args = new DataReceivedEventArgs<T>(data);
        OnDataReceived?.Invoke(this, args);
    }
}