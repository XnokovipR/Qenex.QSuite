using Qenex.QSuite.CoreComm;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Protocol;

public abstract class ProtocolBase : IProtocolBase
{
    public ISpecification Specification { get; } = new SpecificationBase();
    public bool IsEnabled { get; set; } = false;
}