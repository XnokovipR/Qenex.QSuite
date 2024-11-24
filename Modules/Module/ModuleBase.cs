using System.Text.Json.Serialization;
using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.Driver;
using Qenex.QSuite.Specification;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.Module;

public abstract class ModuleBase : IModuleBase
{
    public bool ExitRequested { get; set; } = false;
    
    public bool IsEnabled { get; set; } = false;
    
    public bool IsStarted { get; set; } = false;
    
    public ISpecification Specification { get; init; } = null!;
    
    public IList<IVariableBase> Variables { get; init; } = null!;
    
    public IList<IDriverBase> Drivers { get; init;  } = null!;

    public abstract void AddVariable(IVariableBase variable);
    public abstract void AddVariableRange(IEnumerable<IVariableBase> variables);
    public abstract void RemoveVariable(IVariableBase variable);
    public abstract void AddDriver(IDriverBase driver);
    public abstract void RemoveDriver(IDriverBase driver);

    public abstract Task StartAsync(CancellationToken ct = default);
    public abstract Task StopAsync(CancellationToken ct = default);
    public abstract void Dispose();
    
}