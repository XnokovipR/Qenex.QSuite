namespace Qenex.QSuite.Common.CoreComm;

/// <summary>
/// Common interface to modules, drivers...
/// </summary>
public interface ICoreCommunication : IDisposable
{
    bool IsEnabled { get; set; }
    CommunicationState State { get; }
    string? StateMessage { get; }
    event EventHandler<CommunicationStateChangedEventArgs>? StateChanged;
    
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
