using Qenex.QSuite.Specification;

namespace Qenex.QSuite.CoreModule;

public class CoreModuleBase : ICoreModuleBase
{
    public ISpecification Specification { get; }
}