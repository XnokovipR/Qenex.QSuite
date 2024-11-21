namespace Qenex.QSuite.LogSystem;

public abstract class LogMessage : ILogMessage
{
	public virtual DateTime Timestamp { get; set; }
	public virtual LogLevel Level { get; set; }
	public virtual string Message { get; set; } = String.Empty;
	public virtual Exception? Exception { get; set; }
}