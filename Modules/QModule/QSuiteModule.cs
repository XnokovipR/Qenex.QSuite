using System.Drawing;
using Qenex.QSuite.Driver;
using Qenex.QSuite.Module;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.QModule;

public class QSuiteModule : ModuleBase
{
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
        Driver ??= driver;
    }

    public override void RemoveDriver()
    {
        Driver = null;
    }

    #endregion

    #region Module control

    public override Task StartAsync(CancellationToken ct = default)
    {
        var task = Driver?.StartAsync(ct);
        return task ?? Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken ct = default)
    {
        var task = Driver?.StopAsync(ct);
        return task ?? Task.CompletedTask;
    }

    public override void Dispose()
    {
        Driver?.Dispose();
    }

    #endregion
}