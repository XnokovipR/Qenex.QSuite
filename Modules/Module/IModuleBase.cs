using Qenex.QSuite.Driver;
using Qenex.QSuite.Specification;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.Module;

public interface IModuleBase
{
    ISpecification Specification { get; }
    IList<IVariableBase> Variables { get; }
    IDriverBase? Driver { get; set; }
    
}