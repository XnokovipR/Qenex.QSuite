using System.Reflection;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;

namespace Qenex.QSuite.Drivers.SimDataDriver;

public class SimDriver : DriverBase
{
    #region Fields

    private string settings = string.Empty;
    private string encryptedSettings = string.Empty;
    private volatile bool exitRequested = false;

    private IEnumerable<IProtocolVariable> receivedVariables;
    private int sleepPeriod = 1000;

    #endregion
    
    #region Constructors

    public SimDriver()
    {
        Specification = new SpecificationBase()
        {
            Name = "SimulDataDriver",
            Label = "Simulation Data Driver",
            Description = "Testing driver for simulating data communication.",
            CreatedOn = new DateTime(2025, 2, 1),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
        
        receivedVariables = new List<IProtocolVariable>();
    }

    #endregion

    #region Configuration

    public override void SetConfiguration()
    {
        settings = RawSettings;
        encryptedSettings = RawEncryptedSettings;
        
        var rnd = new Random();
        var rawData = RawSettings.Split(";");
        var numbers = rawData.FirstOrDefault(r => r.Contains("periodes="))?.Split('=')[1].Split(',');
        if (numbers is not { Length: 1 }) throw new Exception("Invalid number of driver periods.");
        
        if (double.TryParse(numbers[0], out var period))
        {
            sleepPeriod = ((int)period);
        }
        
    }

    #endregion

    #region Driver control

    public override async Task StartAsync(CancellationToken ct = default)
    {
        if (!IsEnabled)
        {
            SetState(CommunicationState.Disabled);
            return;
        }

        SetState(CommunicationState.Starting);
        exitRequested = false;
        _ = RunLoopAsync(ct);
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

    #region Communication

    public override void Send<T>(T data)
    {
        throw new NotImplementedException();
    }

    public override Task SendAsync<T>(T data, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    #endregion    
    
    #region Process received data

    private async Task ProcessReceivedDataAsync<T>(T data, CancellationToken ct = default)
    {
        if (data is IEnumerable<IProtocolVariable> variables)
        {
            foreach (var protocol in Protocols)
            {
                if (protocol is not ProtocolBase<int> prot) continue;
                await prot.AddReceivedDataToQueueAsync([sleepPeriod], ct);
            }
        }

        //return Task.CompletedTask;
    }
    
    #endregion

    #region Private

    private async Task RunLoopAsync(CancellationToken ct)
    {
        await Task.Run(async () =>
        {
            foreach (var protocol in Protocols)
            {
                _ = protocol.StartAsync(ct);
            }
            
            SetState(CommunicationState.Running);
            while (!ct.IsCancellationRequested && !exitRequested)
            {
                await ProcessReceivedDataAsync(receivedVariables, ct);
                await Task.Delay(sleepPeriod, ct);
            }
            
            foreach (var protocol in Protocols)
            {
                _ = protocol.StopAsync(ct);
            }
            
            SetState(CommunicationState.Stopped);
        }, ct);
        exitRequested = false;
    } 

    #endregion
}
