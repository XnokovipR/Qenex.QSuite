using Qenex.QSuite.Specification;

namespace Qenex.QSuite.ComponentSpecification;

/// <summary>
/// Basic module specification 
/// </summary>
public interface IComponentSpecification
{
    ISpecification Specification { get; }
}