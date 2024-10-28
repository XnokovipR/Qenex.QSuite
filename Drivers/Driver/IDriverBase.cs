using Qenex.QSuite.CoreComm;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Driver;

public interface IDriverBase : ICoreCommunication
{
    public ISpecification Specification { get; set; }
    public IList<IProtocolBase> Protocols { get; }
    
    public void AddProtocol(IProtocolBase protocol);
    public void RemoveProtocol(IProtocolBase protocol);
    public void RemoveProtocol(string protocolName);
}