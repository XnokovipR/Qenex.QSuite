namespace Qenex.QSuite.Common.CoreComm;

/// <summary>
/// Common interface to modules, drivers...
/// </summary>
public interface ICoreCommunication : IDisposable
{
    bool IsEnabled { get; set; }
    bool IsStarted { get; }
    
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}