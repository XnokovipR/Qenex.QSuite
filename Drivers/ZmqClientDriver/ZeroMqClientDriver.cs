using System.Reflection;
using Qenex.QSuite.Driver;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Drivers.ZmqClientDriver;

/// <summary>
/// ZmqClientDriver is a driver that communicates as a client with the ZeroMQ library (with server). 
/// </summary>
public class ZeroMqClientDriver : DriverBase
{
    #region Fields

    private string settings = string.Empty;
    private string encryptedSettings = string.Empty;
    private volatile bool exitRequested = false;

    #endregion
    
    #region Constructors

    public ZeroMqClientDriver()
    {
        Specification = new SpecificationBase()
        {
            Name = "ZeroMQ Client Driver",
            Description = "ZeroMQ Client Driver",
            CreatedOn = new DateTime(2021, 11, 21),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 1),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    #endregion

    public override void SetConfiguration(string rawSettings, string rawEncryptedSettings)
    {
        settings = $"zmq-client: {rawSettings}";
        encryptedSettings = $"encrypted-zmq-client: {rawEncryptedSettings}";
    }

    #region Driver control

    public override async Task StartAsync(CancellationToken ct = default)
    {
        if (!IsEnabled) return;
        
        exitRequested = false;
        await RunClientAsync(ct);
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
        
    }

    public override Task SendAsync<T>(T data, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Private methods

    // All communication with the ZeroMQ library should be done in this method
    private async Task RunClientAsync(CancellationToken ct)
    {
        await Task.Run(async () =>
        {
            IsStarted = true;
            while (!ct.IsCancellationRequested && !exitRequested)
            {
                // Do something
                await Task.Delay(1000, ct);
                Console.WriteLine("ZeroMqClientDriver is running...");
                //RaiseOnDataReceive("ddd");
            }
            
            IsStarted = false;
        }, ct);
        exitRequested = false;
    }

    #endregion
}