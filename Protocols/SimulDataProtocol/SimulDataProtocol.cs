using System.Collections.Concurrent;
using System.Reflection;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.SimulDataProtocol;

public class SimulDataProtocol : ProtocolBase<int>
{
    #region Fields
    
    private volatile bool exitRequested = false;
    private readonly EventWaitHandle waitHandle;
    private readonly ConcurrentQueue<int> receivedDataQueue;
    
    #endregion
    
    #region Constructors

    public SimulDataProtocol()
    {
        waitHandle = new AutoResetEvent(false);
        receivedDataQueue = new ConcurrentQueue<int>();
        
        Specification = new SpecificationBase()
        {
            Name = "SimulDataProtocol",
            Label = "Simulation Data Protocol",
            Description = "Protocol for simulating data communication. Use especially for testing with the SimDataDriver.",
            CreatedOn = new DateTime(2021, 11, 23),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? throw new Exception("Version not found"),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    #endregion
    
    #region Configuration

    public override void SetConfiguration()
    {
    }

    #endregion

    #region Protocol variables

    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, string commParams, bool isCommunicated)
    {
        throw new NotSupportedException();
    }
    
    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IVarEvent varEvent, string id)
    {
        try
        {
            var protocolVariable = new SimulDataProtocolVariable
            {
                Variable = variable,
                IsCommunicated = true,
                ProtocolVariableSpecification = SimulDataProtocolVariableSpecification.CreateDefault(varEvent!, id)
            };
    
            return protocolVariable; 
        }
        catch (Exception e)
        {
            Logger?.Log(LogLevel.Warn, $"Protocol variable specification for variable {variable.Name} could not be created (${e.Message}).");
            return null;
        }
       
    }
    
    
    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IEnumerable<IVarEvent> variableEvents,
        string commParams, bool isCommunicated )
    {
        try
        {
            var eventRefName = commParams.Split(';').FirstOrDefault(e => e.Contains("eventRef"));
            eventRefName = eventRefName?.Split('=')[1].Trim('"');
        
            var varEvent = variableEvents.FirstOrDefault(e => e.Name == eventRefName);
        
            var protocolVariable = new SimulDataProtocolVariable
            {
                Variable = variable,
                IsCommunicated = isCommunicated,
                ProtocolVariableSpecification = SimulDataProtocolVariableSpecification.Create(varEvent!, commParams)
            };

            return protocolVariable; 
        }
        catch (Exception e)
        {
            Logger?.Log(LogLevel.Warn, $"Protocol variable specification for variable {variable.Name} could not be created (${e.Message}).");
            return null;
        }
       
    }

    #endregion
    
    #region Protocol control

    public override Task StartAsync(CancellationToken ct = default)
    {
        if (!IsEnabled)
        {
            SetState(CommunicationState.Disabled);
            return Task.CompletedTask;
        }

        SetState(CommunicationState.Starting);
        exitRequested = false;
        _ = RunLoopAsync(ct);

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken ct = default)
    {
        SetState(CommunicationState.Stopping);
        exitRequested = true;
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        
    }

    #endregion

    public override Task AddReceivedDataToQueueAsync(IEnumerable<int> data, CancellationToken ct = default)
    {
        foreach (var d in data)
        {
            receivedDataQueue.Enqueue(d);
        }

        waitHandle.Set();
        return Task.CompletedTask;
    }

    protected override void ProcessReceivedData(IEnumerable<int> data)
    {
        var decodedVars = Decode(data);
        foreach (var variable in decodedVars)
        {
            variable.NotifyValueChanged();
        }
    }

    protected override async Task ProcessReceivedDataAsync(IEnumerable<int> data, CancellationToken ct = default)
    {
        var decodedVars = Decode(data);
        var notifyTasks = decodedVars.Select(variable => variable.NotifyValueChangedAsync()).ToList();

        await Task.WhenAll(notifyTasks);
    }

    #region Encoding and decoding

    protected override IEnumerable<int> Encode(IEnumerable<IProtocolVariable> protocolVariables)
    {
        throw new NotImplementedException();
    }

    protected override IEnumerable<IProtocolVariable> Decode(IEnumerable<int> data)
    {
        var protVars = new List<IProtocolVariable>();

        var periods = data.ToList();
        if (periods.Count == 0) return protVars;
        var period = periods[0] as int? ?? 0;
        var rnd = new Random();
        var vars = Variables
            .Where(v => v.IsCommunicated && v.ProtocolVariableSpecification is SimulDataProtocolVariableSpecification)
            .Cast<SimulDataProtocolVariable>();
                    
        foreach (var simpleProtVariable in vars)
        {
            if (simpleProtVariable.Variable is not ScalarVariable scalarVariable) continue;
            if (simpleProtVariable.ProtocolVariableSpecification is not SimulDataProtocolVariableSpecification spec) continue;
            if (spec.VariableEvent is not PeriodicVarEvent periodicVarEvent) continue;
            var resultPeriod = (int)periodicVarEvent.Unit * periodicVarEvent.Period;
            if (resultPeriod != period) continue;

            scalarVariable.Timestamp = DateTime.UtcNow;
            if (scalarVariable.Values is Values<int> intValues)
            {
                intValues.Value = (int)(1000 * Math.Sin(DateTime.UtcNow.Second / 60.0 * 16 * Math.PI)) + (int)(100 * rnd.NextDouble() - 50.0);
            }
            else if (scalarVariable.Values is Values<float> floatValues)
            {
                floatValues.Value = (float)(750 * Math.Sin(DateTime.UtcNow.Second / 60.0 * 8 * Math.PI));
            }
            else if (scalarVariable.Values is Values<byte> byteValues)
            {
                byteValues.Value = (byte)rnd.Next(0, 255);
            }
            
            protVars.Add(simpleProtVariable);
        }           

        return protVars;
    }

    #endregion
    
    #region Private

    private async Task RunLoopAsync(CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            SetState(CommunicationState.Running);
            while (!ct.IsCancellationRequested && !exitRequested)
            {
                if (receivedDataQueue.Count > 0)
                {
                    receivedDataQueue.TryDequeue(out int rcvData);
                    await ProcessReceivedDataAsync(new List<int> { rcvData }, ct);
                }
                else
                {
                    waitHandle.WaitOne();
                }               

            }
            
            SetState(CommunicationState.Stopped);
        }, ct);
        exitRequested = false;

    } 

    #endregion
}
