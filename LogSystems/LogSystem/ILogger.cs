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
    /// Log of a concrete message
    /// </summary>
    /// <param name="level">Level of the message, Error, Warning, Info..</param>
    /// <param name="message">Message to be logged</param>
    /// <param name="exception">Exception to be logged</param>
    void Log(LogLevel level, string message, Exception? exception = default);

    /// <summary>
    /// Asynchronously log message to all registered subscribers.
    /// </summary>
    /// <param name="message">Logged message</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    Task LogAsync(ILogMessage message, CancellationToken ct);
    
    /// <summary>
    /// Log of a concrete message
    /// </summary>
    /// <param name="level">Level of the message, Error, Warning, Info..</param>
    /// <param name="message">Message to be logged</param>
    /// /// <param name="exception">Exception to be logged</param>
    /// <param name="ct">Cancellation token</param>
    Task LogAsync(LogLevel level, string message, Exception? exception = default, CancellationToken ct = default);

    void Enable();

    void Disable();
}