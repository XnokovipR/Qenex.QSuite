using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.Driver;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Specification;
using Qenex.QSuite.QVariables;

namespace Qenex.QSuite.Module;

public abstract class ModuleBase : IModuleBase
{

    #region Constructors

    protected ModuleBase(ILogger? logger = null)
    {
        Logger = logger;
        Variables = new List<IVariableBase>();
        Drivers = new List<IDriverBase>();
    }

    #endregion
    
    #region Properties

    // ReSharper disable once MemberCanBePrivate.Global
    public  ILogger? Logger { get; set; }
    
    public bool ExitRequested { get; set; } = false;
    
    public bool IsEnabled { get; set; } = false;
    
    public bool IsStarted { get; set; } = false;
    
    public ISpecification Specification { get; protected init; }
    
    public IList<IVariableBase> Variables { get; set; }
    
    public IList<IDriverBase> Drivers { get; set;  }

    #endregion

    #region Variables
    public virtual void AddVariable(IVariableBase variable)
    {
        if (Variables.FirstOrDefault(v => v.Id == variable.Id) != null)
        {
            Logger?.Log(LogLevel.Warn, $"Variable with Id {variable.Id} already exists.");
            return;
        }
        
        var highestId = Variables.Count > 0 ? Variables.Max(x => x.Id) : 0;
        if (variable.Id == 0)
        {
            highestId++;
            variable.Id = highestId;
        }
        
        Variables.Add(variable);
    }

    public virtual void AddVariables(IEnumerable<IVariableBase> variables)
    {
        var highestId = Variables.Count > 0 ? Variables.Max(x => x.Id) : 0;
        foreach (var variable in variables)
        {
            if (Variables.FirstOrDefault(v => v.Id == variable.Id) != null)
            {
                Logger?.Log(LogLevel.Warn, $"Variable with Id {variable.Id} already exists.");
                continue;
            }
            
            if (variable.Id == 0)
            {
                highestId++;
                variable.Id = highestId;
            }
            
            Variables.Add(variable);
        }
    }
    
    public virtual void RemoveVariable(IVariableBase variable)
    {
        Variables.Remove(variable);
    }

    #endregion

    #region Driver

    public virtual void AddDriver(IDriverBase driver)
    {
        if (Drivers.FirstOrDefault(v => v.Id == driver.Id) != null)
        {
            Logger?.Log(LogLevel.Warn, $"Driver with Id {driver.Id} already exists.");
            return;
        }
        
        var highestId = Drivers.Count > 0 ? Drivers.Max(x => x.Id) : 0;
        if (driver.Id == 0)
        {
            highestId++;
            driver.Id = highestId;
        }
        Drivers.Add(driver);
    }
    
    public virtual void AddDrivers(IEnumerable<IDriverBase> drivers)
    {
        var highestId = Drivers.Count > 0 ? Drivers.Max(x => x.Id) : 0;
        foreach (var driver in drivers)
        {
            if (Drivers.FirstOrDefault(v => v.Id == driver.Id) != null)
            {
                Logger?.Log(LogLevel.Warn, $"Driver with Id {driver.Id} already exists.");
                return;
            }
            
            if (driver.Id == 0)
            {
                highestId++;
                driver.Id = highestId;
            }
            Drivers.Add(driver);
        }
    }

    public virtual void RemoveDriver(IDriverBase driver)
    {
        Drivers.Remove(driver);
    }

    #endregion

    #region Module control

    public virtual async Task StartAsync(CancellationToken ct = default)
    {
        var tasks = Drivers.Select(driver => driver.StartAsync(ct));
        await Task.WhenAll(tasks);
    }

    public virtual async Task StopAsync(CancellationToken ct = default)
    {
        var tasks = Drivers.Select(driver => driver.StopAsync(ct));
        await Task.WhenAll(tasks);
    }

    public virtual void Dispose()
    {
        foreach (var driver in Drivers)
        {
            driver.Dispose();
        }
    }

    #endregion
    
}