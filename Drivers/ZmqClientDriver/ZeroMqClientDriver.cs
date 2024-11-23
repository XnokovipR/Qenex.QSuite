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
            Gid = new Guid("985a0511-bb95-44a3-889c-95bddbcc7483"),
            Name = "ZeroMQ Client Driver",
            Description = "ZeroMQ Client Driver",
            CreatedOn = new DateTime(2021, 11, 21),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 1),
            Author = "Qenex",
            Company = "QENEX Ltd."
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

    public override void Send<T>(T data)
    {
        
    }

    public override Task SendAsync<T>(T data, CancellationToken ct = default)
    {
        return Task.CompletedTask;
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
                //RaiseOnDataReceive("ddd");
            }
            
            IsStarted = false;
        }, ct);
        ExitRequested = false;
    }
}