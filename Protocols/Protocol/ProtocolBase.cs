using System.ComponentModel;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Specification;
using Qenex.QSuite.QVariables;
using Qenex.QSuite.VariableEvents;

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
        Variables = new List<IProtocolVariable>();
    }

    #endregion

    #region Properties
    
    // ReSharper disable once MemberCanBePrivate.Global
    protected ILogger? Logger { get; set; }

    public int Id { get; set; }
    
    public ISpecification Specification { get; set; }
    
    public IList<IProtocolVariable> Variables { get; set; }

    #endregion

    #region Configuration
    public abstract void SetConfiguration(string rawSettings, string rawEncryptedSettings);
    
    #endregion

    #region Variables

    public abstract IProtocolVariable? CreateProtocolVariable(IVariableBase variable, string commParams, bool isCommunicated);
    public abstract IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IEnumerable<IVarEvent> variableEvents, string commParams, bool isCommunicated);
    
    public virtual void AddVariable(IProtocolVariable protocolVariable)
    {
        if (Variables.FirstOrDefault(v => v.Variable.Id == protocolVariable.Variable.Id) != null)
        {
            Logger?.Log(LogLevel.Warn, $"Variable with Id {protocolVariable.Variable.Id} already exists.");
            return;
        }
        
        var highestId = Variables.Count > 0 ? Variables.Max(x => x.Variable.Id) : 0;
        if (protocolVariable.Variable.Id == 0)
        {
            highestId++;
            protocolVariable.Variable.Id = highestId;
        }
        
        Variables.Add(protocolVariable);
    }
    

    public void RemoveProtocolVariable(IProtocolVariable variable)
    {
        Variables.Remove(variable);
    }

    public void RemoveProtocolVariable(string variableName)
    {
        Variables.Remove(Variables.FirstOrDefault(v => v.Variable.Name == variableName) ??
                         throw new InvalidEnumArgumentException("Variable not found"));
    }

    #endregion

    #region Encode & Decode

    public abstract IEnumerable<T> Encode<T>(IEnumerable<IProtocolVariable> protocolVariables);

    public abstract IEnumerable<IProtocolVariable> Decode<T>(IEnumerable<T> data);

    #endregion
}