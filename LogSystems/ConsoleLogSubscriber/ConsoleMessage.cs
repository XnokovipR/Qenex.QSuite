using Qenex.QSuite.LogSystem;

namespace Qenex.QSuite.ConsoleLogSubscriber;

public sealed class ConsoleMessage : LogMessage
{
	public ConsoleMessage(LogLevel level, string message, Exception? exception = default) : this(DateTime.Now, level, message, exception)
	{
	}
	
	public ConsoleMessage(DateTime timestamp, LogLevel level, string message, Exception? exception = default)
	{
		Timestamp = timestamp;
		Level = level;
		Message = message;
		Exception = exception;
	}

}
