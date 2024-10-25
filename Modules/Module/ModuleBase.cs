using Qenex.QSuite.Driver;
using Qenex.QSuite.Specification;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.Module;

public class ModuleBase : IModuleBase
{
    public ModuleBase(ISpecification specification)
    {
        Specification = specification;
        Variables = new List<IVariableBase>();
        
    }
    
    public ISpecification Specification { get; }
    public IList<IVariableBase> Variables { get; }
    public IDriverBase Driver { get; set; }
}