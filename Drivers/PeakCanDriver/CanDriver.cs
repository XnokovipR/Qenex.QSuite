using System.Reflection;
using Qenex.QSuite.Driver;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Drivers.PeakCanDriver;

public class CanDriver : DriverBase
{
    public CanDriver()
    {
        Specification = new SpecificationBase()
        {
            Name = "Peak CAN Driver",
            Description = "Peak CAN Driver",
            CreatedOn = new DateTime(2021, 11, 21),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 1),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }
    public override Task StartAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public override Task StopAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public override void Dispose()
    {
        throw new NotImplementedException();
    }

    public override void Send<T>(T data)
    {
        throw new NotImplementedException();
    }

    public override Task SendAsync<T>(T data, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}