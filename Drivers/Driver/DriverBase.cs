using System.ComponentModel;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Drivers;

public abstract class DriverBase : IDriverBase
{

    #region Constructors

    protected DriverBase(ILogger? logger = null)
    {
        Logger = logger;
    }

    #endregion

    #region Properties
    
    // ReSharper disable once MemberCanBePrivate.Global
    public  ILogger? Logger { get; set; }
    
    // The same driver can be used in the same module and communicate with different devices.
    // So, the driver should have an Id to distinguish between them.
    public int Id { get; set; }
    public string Label { get; set; } = null!;
    public bool IsEnabled { get; set; }
    
    public bool IsStarted { get; protected set; } = false;
    
    public ISpecification Specification { get; init; } = null!;
    
    public IList<IProtocolBase> Protocols { get; init; } = new List<IProtocolBase>();

    #endregion

    public abstract void SetConfiguration(string rawSettings, string rawEncryptedSettings);

    #region Protocols

    public virtual void AddProtocol(IProtocolBase protocol)
    {
        if (Protocols.FirstOrDefault(v => v.Specification.Name == protocol.Specification.Name) != null)
        {
            Logger?.Log(LogLevel.Warn, $"Protocol with name {protocol.Specification.Name} already exists.");
            return;
        }
        
        if (Protocols.FirstOrDefault(v => v.Id == protocol.Id) != null)
        {
            Logger?.Log(LogLevel.Warn, $"Protocol with Id {protocol.Id} already exists.");
            return;
        }    
        
        var highestId = Protocols.Count > 0 ? Protocols.Max(x => x.Id) : 0;
        if (protocol.Id == 0)
        {
            highestId++;
            protocol.Id = highestId;
        }
        
        Protocols.Add(protocol);
    }
    
    public virtual void AddProtocols(IEnumerable<IProtocolBase> protocols)
    {
        var highestId = Protocols.Count > 0 ? Protocols.Max(x => x.Id) : 0;
        foreach (var protocol in protocols)
        {
            if (Protocols.FirstOrDefault(v => v.Specification.Name == protocol.Specification.Name) != null)
            {
                Logger?.Log(LogLevel.Warn, $"Protocol with name {protocol.Specification.Name} already exists.");
                return;
            }
        
            if (Protocols.FirstOrDefault(v => v.Id == protocol.Id) != null)
            {
                Logger?.Log(LogLevel.Warn, $"Protocol with Id {protocol.Id} already exists.");
                return;
            }    
            
            if (protocol.Id == 0)
            {
                highestId++;
                protocol.Id = highestId;
            }
            
            Protocols.Add(protocol);
        }
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

    #endregion
    
    #region Driver control

    public abstract Task StartAsync(CancellationToken ct = default);

    public abstract Task StopAsync(CancellationToken ct = default);

    public abstract void Dispose();

    #endregion

    #region Communication

    public abstract void Send<T>(T data);

    public abstract Task SendAsync<T>(T data, CancellationToken ct = default);

    public event EventHandler? OnDataReceived;
    
    protected void RaiseOnDataReceive<T>(T data)
    {
        var args = new DataReceivedEventArgs<T>(data);
        OnDataReceived?.Invoke(this, args);
    }

    #endregion
}