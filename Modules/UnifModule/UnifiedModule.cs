using System.Reflection;
using Qenex.QSuite.Driver;
using Qenex.QSuite.Module;
using Qenex.QSuite.Specification;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.UnifModule;

/// <summary>
/// Unified model is common for both QSuiteModel and also for Model working as a independent module executed in service.
/// </summary>
public class UnifiedModule : ModuleBase
{
    public UnifiedModule()
    {
        Specification = new SpecificationBase()
        {
            Gid = new Guid("f1b3b1b3-1b3b-1b3b-1b3b-1b3b1b3b1b3a"),
            Name = "UnifiedModule",
        };
    }
    
    #region Variables

    public override void AddVariable(IVariableBase variable)
    {
        Variables.Add(variable);
    }
    
    public override void AddVariableRange(IEnumerable<IVariableBase> variables)
    {
        foreach (var variable in variables)
        {
            Variables.Add(variable);
        }
    }

    public override void RemoveVariable(IVariableBase variable)
    {
        Variables.Remove(variable);
    }

    #endregion

    #region Driver

    public override void AddDriver(IDriverBase driver)
    {
        Drivers.Add(driver);
    }

    public override void RemoveDriver(IDriverBase driver)
    {
        Drivers.Remove(driver);
    }

    #endregion

    #region Module control

    public override async Task StartAsync(CancellationToken ct = default)
    {
        var tasks = Drivers.Select(driver => driver.StartAsync(ct));
        await Task.WhenAll(tasks);
    }

    public override async Task StopAsync(CancellationToken ct = default)
    {
        var tasks = Drivers.Select(driver => driver.StopAsync(ct));
        await Task.WhenAll(tasks);
    }

    public override void Dispose()
    {
        foreach (var driver in Drivers)
        {
            driver.Dispose();
        }
    }

    #endregion
}