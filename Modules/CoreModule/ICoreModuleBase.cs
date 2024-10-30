using Qenex.QSuite.Specification;

namespace Qenex.QSuite.CoreModule;

public interface ICoreModuleBase
{
    ISpecification Specification { get; }
}