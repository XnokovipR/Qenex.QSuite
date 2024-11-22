using System.Reflection;
using Qenex.QSuite.Driver;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.ZmqClientDriver;

/// <summary>
/// ZmqClientDriver is a driver that communicates as a client with the ZeroMQ library (with server). 
/// </summary>
public class ZeroMqClientDriver : DriverBase
{
    public ZeroMqClientDriver(ILogSubscriber? logSubscriber = null) : base(logSubscriber)
    {
        Specification = new SpecificationBase()
        {
            Gid = new Guid("4f225c91-7f02-4577-bb5e-a9e3a3ead56e"),
            Name = "ZeroMQ Client Driver",
            Description = "ZeroMQ Client Driver",
            ReleaseDate = new DateTime(2021, 11, 21),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 1)
        };
    }

    public override async Task StartAsync(CancellationToken ct = default)
    {
        if (!IsEnabled) return;
        
        ExitRequested = false;
        await RunClientAsync(ct);
    }

    public override Task StopAsync(CancellationToken ct = default)
    {
        ExitRequested = true;
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
    }
    
    // All communication with the ZeroMQ library should be done in this method
    private async Task RunClientAsync(CancellationToken ct)
    {
        await Task.Run(() =>
        {
            IsStarted = true;
            while (!ct.IsCancellationRequested || !ExitRequested)
            {
                // Do something
                Thread.Sleep(100);
            }
            
            IsStarted = false;
        }, ct);
        ExitRequested = false;
    }
}