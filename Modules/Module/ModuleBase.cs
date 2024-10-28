using Qenex.QSuite.Driver;
using Qenex.QSuite.Specification;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.Module;

public abstract class ModuleBase : IModuleBase
{
    public ModuleBase()
    {
        IsEnabled = false;
        IsStarted = false;
        Specification = new Specification.SpecificationBase();
        Variables = new List<IVariableBase>();
    }
    
    public bool IsEnabled { get; set; }
    public bool IsStarted { get; set; }
    public ISpecification Specification { get; }
    public IList<IVariableBase> Variables { get; }
    public IDriverBase? Driver { get; set; }


    public abstract Task StartAsync();
    public abstract Task StopAsync();
    public abstract void Dispose();
    
}