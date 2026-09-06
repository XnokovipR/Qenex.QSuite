using System.ComponentModel;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Specifications.ComponentSpecification;

namespace Qenex.QSuite.Drivers.Driver;

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
    public string RawSettings { get; set; } = string.Empty;
    public string RawEncryptedSettings { get; set; } = string.Empty;

    // Overridden by drivers that have settings; base returns empty (no settings to pre-fill).
    public virtual string DefaultRawSettings => string.Empty;

    public bool IsEnabled { get; set; }
    
    public CommunicationState State { get; private set; } = CommunicationState.Stopped;
    public string? StateMessage { get; private set; }
    public event EventHandler<CommunicationStateChangedEventArgs>? StateChanged;
    
    public ISpecification Specification { get; init; } = null!;
    
    public IList<IProtocolBase> Protocols { get; init; } = new List<IProtocolBase>();

    #endregion

    public abstract void SetConfiguration();

    protected void SetState(CommunicationState state, string? message = null)
    {
        if (State == state && StateMessage == message)
        {
            return;
        }

        var previousState = State;
        State = state;
        StateMessage = message;
        StateChanged?.Invoke(this, new CommunicationStateChangedEventArgs(previousState, state, message));
    }

    /// <summary>
    /// Tell every hosted protocol that the transport connection was lost (connected = false) or
    /// re-established (connected = true). Drivers call this from their connect/reconnect logic so
    /// a protocol with a live session can drop and re-establish it at once, instead of inferring
    /// the loss from repeated command timeouts. A misbehaving protocol handler must not break the
    /// driver's recovery, so exceptions are caught and logged.
    /// </summary>
    protected void NotifyProtocolsTransportConnectionChanged(bool connected)
    {
        foreach (var protocol in Protocols)
        {
            try
            {
                protocol.OnTransportConnectionChanged(connected);
            }
            catch (Exception e)
            {
                Logger?.Log(LogLevel.Warn,
                    $"Protocol '{protocol.Specification?.Name}' OnTransportConnectionChanged({connected}) threw: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Stops every hosted protocol. Drivers call this twice per stop on purpose: first from their
    /// StopAsync while the receive path is still alive — a session protocol such as an XCP master
    /// sends DISCONNECT here and needs the transport to deliver the slave's response — and again
    /// from the run loop teardown, which also covers the abnormal paths (transport lost, loop
    /// failed). Protocol StopAsync implementations are idempotent. A failing protocol must not
    /// keep the driver from stopping, so exceptions are caught and logged.
    /// </summary>
    protected async Task StopProtocolsAsync()
    {
        foreach (var protocol in Protocols)
        {
            try
            {
                await protocol.StopAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                Logger?.Log(LogLevel.Warn,
                    $"Protocol '{protocol.Specification?.Name}' failed to stop: {e.Message}");
            }
        }
    }

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

        protocol.Logger ??= Logger;
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

            protocol.Logger ??= Logger;
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

    //public event EventHandler? OnDriverDataReceived;
    
    // protected void RaiseOnDataReceive<T>(T data)
    // {
    //     var args = new DataReceivedEventArgs<T>(data);
    //     OnDriverDataReceived?.Invoke(this, args);
    // }

    #endregion
}
