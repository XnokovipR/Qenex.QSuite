using Qenex.QSuite.CoreComm;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Protocol;

public interface IProtocolBase : ICoreCommunication
{
    ISpecification Specification { get;  }
}