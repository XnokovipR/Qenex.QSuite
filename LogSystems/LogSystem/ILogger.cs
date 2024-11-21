namespace Qenex.QSuite.LogSystem;

/// <summary>
/// ILogger is a general interface for logging system. 
/// </summary>
public interface ILogger
{
    void RegisterSubscriber(ILogSubscriber subscriber);

    void UnRegisterSubscriber(ILogSubscriber subscriber);

    /// <summary>
    /// Log message to all registered subscribers.
    /// </summary>
    /// <param name="message">logged message</param>
    void Log(ILogMessage message);

    /// <summary>
    /// Asynchronously log message to all registered subscribers.
    /// </summary>
    /// <param name="message">Logged message</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    Task LogAsync(ILogMessage message, CancellationToken ct);

    void Enable();

    void Disable();
}