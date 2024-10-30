using System.ComponentModel;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Driver;

public abstract class DriverBase : IDriverBase
{
    public bool IsEnabled { get; set; }
    public bool IsStarted { get; set; }
    
    public DriverBase(ISpecification specification)
    {
        Specification = specification;
        Protocols = new List<IProtocolBase>();
    }

    public ISpecification Specification { get; set; }
    public IList<IProtocolBase> Protocols { get; }
    
    public abstract Task StartAsync(CancellationToken ct = default);
    public abstract Task StopAsync(CancellationToken ct = default);
    public abstract void Dispose();
    
    public virtual void AddProtocol(IProtocolBase protocol)
    {
        Protocols.Add(protocol);
    }

    public void RemoveProtocol(IProtocolBase protocol)
    {
        Protocols.Remove(protocol);
    }

    public void RemoveProtocol(string protocolName)
    {
        Protocols.Remove(Protocols.FirstOrDefault(p => p.Specification.Name == protocolName) ??
                         throw new InvalidEnumArgumentException("Protocol not found"));
    }
}