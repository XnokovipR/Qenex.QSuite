using System.Reflection;
using NetMQ;
using NetMQ.Sockets;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Specifications.Specification;

namespace Qenex.QSuite.Drivers.ZmqServerDriver;

public class ZeroMqServerDriver : DriverBase
{
    #region Fields
    
    private string settings = string.Empty;
    private string encryptedSettings = string.Empty;
    
    private ResponseSocket sendSocket;
    private RequestSocket receiveSocket;
    private NetMQPoller poller;

    #endregion
    
    #region Constructors

    public ZeroMqServerDriver()
    {
        Specification = new SpecificationBase()
        {
            Name = "ZeroMQServerDriver",
            Label = "ZeroMQ Server Driver",
            Description = "ZeroMQ Server Driver",
            CreatedOn = new DateTime(2024, 12, 25),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    #endregion

    #region Configuration

    public override void SetConfiguration()
    {
        settings = $"zmq-server: {RawSettings}";
        encryptedSettings = $"encrypted-zmq-server: {RawEncryptedSettings}";
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

        try
        {
            SetState(CommunicationState.Starting);
            sendSocket = new ResponseSocket();
            
            receiveSocket = new RequestSocket();
            receiveSocket.ReceiveReady += (sender, args) =>
            {
                var message = args.Socket.ReceiveFrameString();
                Logger?.Log(LogLevel.Info, $"Received message: {message}");
            };
            poller = new NetMQPoller {receiveSocket};

            await RunClientAsync(ct);
        }
        catch (Exception e)
        {
            SetState(CommunicationState.Faulted, e.Message);
            Logger?.Log(LogLevel.Error, "ZeroMqServerDriver failed to start.", e);
        }
    }

    public override Task StopAsync(CancellationToken ct = default)
    {
        try
        {
            SetState(CommunicationState.Stopping);
            poller.Stop();
            SetState(CommunicationState.Stopped);
        }
        catch (Exception e)
        {
            SetState(CommunicationState.Faulted, e.Message);
            Logger?.Log(LogLevel.Error, "ZeroMqServerDriver failed to stop.", e);
        }
        
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        poller.Dispose();
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
        await Task.Run(() =>
        {
            SetState(CommunicationState.Running);

            poller.Run();
            
            SetState(CommunicationState.Stopped);
        }, ct);
    }

    #endregion
}
