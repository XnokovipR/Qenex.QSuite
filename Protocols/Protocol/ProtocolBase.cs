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
        Variables = new List<IProtocolVariable>();
        Logger = logger;
    }

    #endregion

    #region Properties
    
    public bool IsEnabled { get; set; }
    
    public CommunicationState State { get; private set; } = CommunicationState.Stopped;
    public string? StateMessage { get; private set; }
    public event EventHandler<CommunicationStateChangedEventArgs>? StateChanged;
    
    private ILogger? logger;

    /// <summary>
    /// Protocol-level logger. Setting it also flows to variables added earlier, so
    /// diagnostics work regardless of whether variables were added before or after
    /// the owning driver propagated its logger.
    /// </summary>
    public ILogger? Logger
    {
        get => logger;
        set
        {
            logger = value;
            foreach (var protocolVariable in Variables.OfType<ProtocolVariable>())
            {
                protocolVariable.Logger ??= value;
            }
        }
    }
    
    public int Id { get; set; }
    
    public ISpecification Specification { get; set; }
    public string RawSettings { get; set; } = string.Empty;
    public string RawEncryptedSettings { get; set; } = string.Empty;

    // Overridden by protocols that have protocol-level settings; base returns empty.
    public virtual string DefaultRawSettings => string.Empty;

    // Overridden only by protocols tied to one concrete driver (e.g. paired examples);
    // base returns null = any type-compatible driver.
    public virtual IReadOnlyList<string>? CompatibleDrivers => null;

    public IList<IProtocolVariable> Variables { get; set; }

    #endregion

    #region Configuration
    public abstract void SetConfiguration();

    // The common vocabulary understood by most protocols; protocols with their own
    // per-variable parameters override this with a template listing all of them.
    public virtual string CreateDefaultCommParam(IVariableBase variable, IEnumerable<IVarEvent> variableEvents)
    {
        var eventName = GetDefaultEventName(variableEvents);
        var parameters = new List<string>
        {
            "direction=\"read\""
        };

        if (!string.IsNullOrWhiteSpace(eventName))
        {
            parameters.Add($"eventRef=\"{eventName}\"");
        }

        parameters.Add("multiplier=\"1\"");
        parameters.Add($"id=\"{variable.Name}\"");
        return string.Join(";", parameters);
    }

    /// <summary>First periodic event of the configuration, or the first event, or empty.</summary>
    protected static string GetDefaultEventName(IEnumerable<IVarEvent> variableEvents)
    {
        return variableEvents.FirstOrDefault(variableEvent => variableEvent is PeriodicVarEvent)?.Name
               ?? variableEvents.FirstOrDefault()?.Name
               ?? string.Empty;
    }

    protected void SetState(CommunicationState state, string? message = null)
    {
        if (State == state && StateMessage == message)
        {
            return;
        }

        var previousState = State;
        State = state;
        StateMessage = message;
        StateChanged?.Invoke(this, new CommunicationStateChangedEventArgs(previousState, state, message));
    }
    
    #endregion

    #region Variables

    public abstract IProtocolVariable? CreateProtocolVariable(IVariableBase variable, string commParams, bool isCommunicated);
    public abstract IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IVarEvent variableEvent, string id);
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

        // Diagnostics: so ProtocolVariable can log otherwise-swallowed subscriber exceptions.
        if (protocolVariable is ProtocolVariable concreteProtocolVariable)
        {
            concreteProtocolVariable.Logger = Logger;
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
