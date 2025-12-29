using System.Collections.Concurrent;
using System.Reflection;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.SimpleProtocol;

public class SimpleOne2OneProtocol : ProtocolBase<int>
{
    #region Fields
    
    private volatile bool exitRequested = false;
    private readonly EventWaitHandle waitHandle;
    private readonly ConcurrentQueue<int> receivedDataQueue;
    
    #endregion
    
    #region Constructors

    public SimpleOne2OneProtocol()
    {
        waitHandle = new AutoResetEvent(false);
        receivedDataQueue = new ConcurrentQueue<int>();
        
        Specification = new SpecificationBase()
        {
            Name = "SimpleO2OProtocol",
            Label = "Simple One to One Protocol",
            Description = "No conversion of data is done. Data is sent as is.",
            CreatedOn = new DateTime(2021, 11, 23),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? throw new Exception("Version not found"),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    #endregion
    
    #region Configuration

    public override void SetConfiguration(string rawSettings, string rawEncryptedSettings)
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
            var protocolVariable = new SimpleOne2OneProtocolVariable
            {
                Variable = variable,
                IsCommunicated = true,
                ProtocolVariableSpecification = SimpleProtVariableSpecification.CreateDefault(varEvent!, id)
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
        
            var protocolVariable = new SimpleOne2OneProtocolVariable
            {
                Variable = variable,
                IsCommunicated = isCommunicated,
                ProtocolVariableSpecification = SimpleProtVariableSpecification.Create(varEvent!, commParams)
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
        if (!IsEnabled) return Task.CompletedTask;
        exitRequested = false;
        _ = RunLoopAsync(ct);

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken ct = default)
    {
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
        var notifyTasks = new List<Task>();
        
        foreach (var variable in decodedVars)
        {
            notifyTasks.Add(variable.NotifyValueChangedAsync());
        }
        
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
            .Where(v => v.ProtocolVariableSpecification is SimpleProtVariableSpecification).Cast<SimpleOne2OneProtocolVariable>();
                    
        foreach (var simpleProtVariable in vars)
        {
            if (simpleProtVariable.Variable is not ScalarVariable scalarVariable) continue;
            if (simpleProtVariable.ProtocolVariableSpecification is not SimpleProtVariableSpecification spec) continue;
            if (spec.VariableEvent is not PeriodicVarEvent periodicVarEvent) continue;
            if (periodicVarEvent.Period != period) continue;
                        
            if (scalarVariable.Values is Values<int> intValues)
            {
                intValues.Value = rnd.Next(-20, 20);
            }
            else if (scalarVariable.Values is Values<float> floatValues)
            {
                floatValues.Value = 10 * (float)rnd.NextDouble();
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
            IsStarted = true;
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
            
            IsStarted = false;
        }, ct);
        exitRequested = false;

    } 

    #endregion
}