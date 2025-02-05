using System.Reflection;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Drivers.PeakCanDriver;

public class CanDriver : DriverBase
{
    #region Fields

    private string settings = string.Empty;
    private string encryptedSettings = string.Empty;

    #endregion
    
    #region Constructors

    public CanDriver()
    {
        Specification = new SpecificationBase()
        {
            Name = "PeakCANDriver",
            Label = "Peak CAN Driver",
            Description = "Peak CAN Driver",
            CreatedOn = new DateTime(2021, 11, 21),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    #endregion

    public override void SetConfiguration(string rawSettings, string rawEncryptedSettings)
    {
        settings = $"can: {rawSettings}";
        encryptedSettings = $"encrypted-can: {rawEncryptedSettings}";
    }

    #region Driver control

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
        throw new NotImplementedException();
    }
    #endregion
}