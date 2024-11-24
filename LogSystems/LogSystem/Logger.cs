namespace Qenex.QSuite.LogSystem;

/// <summary>
/// Logger servers as a main logger system which logs all incoming messages to all registered subscribers.
/// </summary>
public class Logger : ILogger
{
	private readonly List<ILogSubscriber> subscribers;
	private bool isLogEnabled = true;
	private LogLevel minimumLogLevel = LogLevel.Warn;

	public Logger(LogLevel minLogLevel)
	{
		minimumLogLevel = minLogLevel;
		subscribers = new List<ILogSubscriber>();
	}

	public void RegisterSubscriber(ILogSubscriber subscriber)
	{
		subscribers.Add(subscriber);
	}


	public void UnRegisterSubscriber(ILogSubscriber subscriber)
	{
		subscribers.Remove(subscriber);
	}

	public void Log(ILogMessage message)
	{
		if (!isLogEnabled || message.Level < minimumLogLevel)
		{
			return;
		}
		
		foreach (var subscriber in subscribers)
		{
			subscriber.Log(message);
		}
	}

	public void Log(LogLevel level, string message, Exception? exception = default)
	{
		Log(new LogMessage(level, message, exception));
	}

	public async Task LogAsync(ILogMessage message, CancellationToken ct = default)
	{
		if (!isLogEnabled || message.Level < minimumLogLevel)
		{
			return;
		}

		foreach (var subscriber in subscribers)
		{
			await subscriber.LogAsync(message, ct);
		}
	}

	public async Task LogAsync(LogLevel level, string message, Exception? exception = default, CancellationToken ct = default)
	{
		await LogAsync(new LogMessage(level, message, exception), ct);
	}

	public void Enable()
	{
		isLogEnabled = true;
	}

	public void Disable()
	{
		isLogEnabled = false;
	}
}