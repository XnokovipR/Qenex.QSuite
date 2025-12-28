using System.ComponentModel;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.Protocol;

/// <summary>
/// Base class for all protocols.
/// </summary>
public abstract class ProtocolBase<T> : IProtocolBase
{
    #region Constructors

    public ProtocolBase(ILogger? logger = null)
    {
        Logger = logger;
        Variables = new List<IProtocolVariable>();
    }

    #endregion

    #region Properties
    
    public bool IsEnabled { get; set; }
    
    public bool IsStarted { get; protected set; } = false;
    
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

    #region Protocol control

    public abstract Task StartAsync(CancellationToken ct = default);

    public abstract Task StopAsync(CancellationToken ct = default);

    public abstract void Dispose();
    

    #endregion
    
    #region Process received data

    public abstract Task AddReceivedDataToQueueAsync(IEnumerable<T> data, CancellationToken ct = default);

    protected abstract void ProcessReceivedData(IEnumerable<T> data);

    protected abstract Task ProcessReceivedDataAsync(IEnumerable<T> data, CancellationToken ct = default);

    #endregion

    #region Encode & Decode

    protected abstract IEnumerable<T> Encode(IEnumerable<IProtocolVariable> protocolVariables);

    protected abstract IEnumerable<IProtocolVariable> Decode(IEnumerable<T> data);

    #endregion
}