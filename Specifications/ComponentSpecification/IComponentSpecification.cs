using Qenex.QSuite.Specifications.Specification;

namespace Qenex.QSuite.Specifications.ComponentSpecification;

/// <summary>
/// Basic module specification 
/// </summary>
public interface IComponentSpecification
{
    ISpecification Specification { get; }
}