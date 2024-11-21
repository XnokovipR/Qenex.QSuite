using Qenex.QSuite.LogSystem;

namespace Qenex.QSuite.NLogSubscriber;

public sealed class NLogMessage: LogMessage
{
    public NLogMessage(DateTime timestamp, LogLevel level, string message, Exception? exception = null)
    {
        Timestamp = timestamp;
        Level = level;
        Message = message;
        Exception = exception;
    }
}