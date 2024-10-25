using System.ComponentModel;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Driver;

public class DriverBase : IDriverBase
{
    public DriverBase(ISpecification specification)
    {
        Specification = specification;
        Protocols = new List<IProtocolBase>();
    }

    public ISpecification Specification { get; set; }
    public IList<IProtocolBase> Protocols { get; }
    
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