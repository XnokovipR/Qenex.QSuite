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
        ProtocolVariables = new List<IProtocolVariable>();
    }

    #endregion


    #region Properties
    
    // ReSharper disable once MemberCanBePrivate.Global
    public  ILogger? Logger { get; set; }

    public int Id { get; set; }
    
    public ISpecification Specification { get; set; }
    
    public IList<IProtocolVariable> ProtocolVariables { get; set; }

    #endregion

    #region Variables

    public virtual void AddVariable(IProtocolVariable variable)
    {
        if (ProtocolVariables.FirstOrDefault(v => v.Id == variable.Id) != null)
        {
            Logger?.Log(LogLevel.Warn, $"Variable with Id {variable.Id} already exists.");
            return;
        }
        
        var highestId = ProtocolVariables.Count > 0 ? ProtocolVariables.Max(x => x.Id) : 0;
        if (variable.Id == 0)
        {
            highestId++;
            variable.Id = highestId;
        }
        
        ProtocolVariables.Add(variable);
    }
    public virtual void AddVariables(IEnumerable<IProtocolVariable> variables)
    {
        var highestId = ProtocolVariables.Count > 0 ? ProtocolVariables.Max(x => x.Id) : 0;
        foreach (var variable in variables)
        {
            if (ProtocolVariables.FirstOrDefault(v => v.Id == variable.Id) != null)
            {
                Logger?.Log(LogLevel.Warn, $"Variable with Id {variable.Id} already exists.");
                continue;
            }
            
            if (variable.Id == 0)
            {
                highestId++;
                variable.Id = highestId;
            }
            
            ProtocolVariables.Add(variable);
        }
    }

    public void RemoveVariable(IProtocolVariable variable)
    {
        ProtocolVariables.Remove(variable);
    }

    public void RemoveVariable(string variableName)
    {
        ProtocolVariables.Remove(ProtocolVariables.FirstOrDefault(v => v.Name == variableName) ??
                                 throw new InvalidEnumArgumentException("Variable not found"));
    }

    #endregion
}