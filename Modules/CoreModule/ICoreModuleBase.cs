using Qenex.QSuite.Specification;

namespace Qenex.QSuite.CoreModule;

/// <summary>
/// Basic module specification 
/// </summary>
public interface ICoreModuleBase
{
    ISpecification Specification { get; }
}