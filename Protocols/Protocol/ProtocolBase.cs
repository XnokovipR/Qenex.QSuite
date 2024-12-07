using System.ComponentModel;
using Qenex.QSuite.CoreComm;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Specification;
using Qenex.QSuite.QVariables;

namespace Qenex.QSuite.Protocol;

/// <summary>
/// Base class for all protocols.
/// </summary>
public abstract class ProtocolBase : IProtocolBase
{
    #region Constructors

    public ProtocolBase(ILogger? logger = null)
    {
        Logger = logger;
        Variables = new List<IVariableBase>();
    }

    #endregion


    #region Properties
    
    // ReSharper disable once MemberCanBePrivate.Global
    public  ILogger? Logger { get; set; }

    public int Id { get; set; }
    
    public ISpecification Specification { get; set; }
    
    public IList<IVariableBase> Variables { get; set; }

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

    public void RemoveVariable(IVariableBase variable)
    {
        Variables.Remove(variable);
    }

    public void RemoveVariable(string variableName)
    {
        Variables.Remove(Variables.FirstOrDefault(v => v.Name == variableName) ??
                                 throw new InvalidEnumArgumentException("Variable not found"));
    }

    #endregion
}