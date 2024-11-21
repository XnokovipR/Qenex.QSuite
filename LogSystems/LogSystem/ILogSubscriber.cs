namespace Qenex.QSuite.LogSystem;

/// <summary>
/// Interface for log subscribers.
/// </summary>
public interface ILogSubscriber
{
	/// <summary>
	/// Log of a concrete message 
	/// </summary>
	/// <param name="message">Logged message</param>
	void Log(ILogMessage message);
	
	/// <summary>
	/// Asynchronously log of a concrete message
	/// </summary>
	/// <param name="message">Logged message</param>
	/// <param name="ct">Cancellation token</param>
	/// <returns></returns>
	Task LogAsync(ILogMessage message, CancellationToken ct);
}