using Qenex.QSuite.LogSystems.LogSystem;
using Logger = NLog.Logger;
using LogSystem = Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QSuite.LogSystems.NLogSubscriber;

/// <summary>
/// NLogSubscriber logs message via NLog library into files.
/// </summary>
public class NLogSubscriber : ILogSubscriber
{
	private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();

	private static readonly Dictionary<LogSystem.LogLevel, Action<Exception, string>> LogLevelToActionConverter = new()
	{
		{LogSystem.LogLevel.Trace, logger.Trace},
		{LogSystem.LogLevel.Debug, logger.Debug},
		{LogSystem.LogLevel.Info, logger.Info},
		{LogSystem.LogLevel.Warn, logger.Warn},
		{LogSystem.LogLevel.Error, logger.Error},
		{LogSystem.LogLevel.Fatal, logger.Fatal}
	};

	public void Log(ILogMessage message)
	{
		LogMessage(message);
	}

	public async Task LogAsync(ILogMessage message, CancellationToken ct = default)
	{
		await Task.Run(() => { LogMessage(message); }, ct);
	}

	private void LogMessage(ILogMessage message)
	{
		var msg = message is LogMessage ? "NLogMessage: " + message.Message : message.Message;
		LogLevelToActionConverter[message.Level]?.Invoke(message.Exception, msg);
	}
}