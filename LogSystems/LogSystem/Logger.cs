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

	public void Enable()
	{
		isLogEnabled = true;
	}

	public void Disable()
	{
		isLogEnabled = false;
	}
}