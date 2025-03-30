using System.Reflection;
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
            Description = "Peak CAN Driver",
            CreatedOn = new DateTime(2025, 2, 1),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
        
        receivedVariables = new List<IProtocolVariable>();
    }

    #endregion

    #region Configuration

    public override void SetConfiguration(string rawSettings, string rawEncryptedSettings)
    {
        settings = rawSettings;
        encryptedSettings = rawEncryptedSettings;
        
        var rnd = new Random();
        var rawData = rawSettings.Split(";");
        var numbers = rawData.FirstOrDefault(r => r.Contains("periodes="))?.Split('=')[1].Split(',');
        if (numbers != null && numbers.Length == 1)
        {
            if (int.TryParse(numbers[0], out var period))
            {
                sleepPeriod = 1000 * period;
            }
        }
    }

    #endregion

    #region Driver control

    public override async Task StartAsync(CancellationToken ct = default)
    {
        if (!IsEnabled) return;
        exitRequested = false;
        await RunLoopAsync(ct);
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
    protected override void ProcessReceivedData<T>(T data)
    {
        throw new NotImplementedException();
    }

    protected override Task ProcessReceivedDataAsync<T>(T data, CancellationToken ct = default)
    {
        if (data is IEnumerable<IProtocolVariable> variables)
        {
            this.RaiseOnDataReceive(data);
        }

        return Task.CompletedTask;
    }
    
    #endregion

    #region Private

    private async Task RunLoopAsync(CancellationToken ct)
    {
        await Task.Run(async () =>
        {
            IsStarted = true;
            while (!ct.IsCancellationRequested && !exitRequested)
            {
                receivedVariables = Protocols[0].Decode([sleepPeriod / 1000]);
                await ProcessReceivedDataAsync(receivedVariables, ct);
                await Task.Delay(sleepPeriod, ct);
            }
            
            IsStarted = false;
        }, ct);
        exitRequested = false;

    } 

    #endregion
}