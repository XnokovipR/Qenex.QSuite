namespace Qenex.QSuite.LogSystems.LogSystem;

/// <summary>
/// General ILogger message interface.
/// </summary>
public interface ILogMessage
{
	DateTime Timestamp { get; }
	LogLevel Level { get; }
	string Message { get; }
	public Exception? Exception { get; }
}