namespace Qenex.QSuite.CoreComm;

public interface ICoreCommunication : IDisposable
{
    bool IsEnabled { get; set; }
    bool IsStarted { get; set; }
    
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}