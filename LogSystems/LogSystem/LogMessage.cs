namespace Qenex.QSuite.LogSystem;

public class LogMessage : ILogMessage
{
	public LogMessage(LogLevel level, string message, Exception? exception = default) : this(DateTime.Now, level, message, exception)
	{
	}
	
	public LogMessage(DateTime timestamp, LogLevel level, string message, Exception? exception = default)
	{
		Timestamp = timestamp;
		Level = level;
		Message = message;
		Exception = exception;
	}
	
	public virtual DateTime Timestamp { get; }
	public virtual LogLevel Level { get; }
	public virtual string Message { get; } = string.Empty;
	public virtual Exception? Exception { get; }
}