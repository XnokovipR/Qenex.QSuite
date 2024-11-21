namespace Qenex.QSuite.CoreComm;

/// <summary>
/// Common interface to modules, drivers...
/// </summary>
public interface ICoreCommunication : IDisposable
{
    bool ExitRequested { get; set; }
    bool IsEnabled { get; set; }
    bool IsStarted { get; set; }
    
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}