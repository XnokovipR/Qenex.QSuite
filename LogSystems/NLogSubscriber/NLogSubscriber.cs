using Qenex.QSuite.LogSystem;
using NLog;
using Logger = NLog.Logger;
using SystemLog = Qenex.QSuite.LogSystem;

namespace Qenex.QSuite.NLogSubscriber;

/// <summary>
/// NLogSubscriber logs message via NLog library into files.
/// </summary>
public class NLogSubscriber : ILogSubscriber
{
	private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();

	private static readonly Dictionary<SystemLog.LogLevel, Action<Exception, string>> logLevelToActionConverter = new()
	{
		{SystemLog.LogLevel.Trace, logger.Trace},
		{SystemLog.LogLevel.Debug, logger.Debug},
		{SystemLog.LogLevel.Info, logger.Info},
		{SystemLog.LogLevel.Warn, logger.Warn},
		{SystemLog.LogLevel.Error, logger.Error},
		{SystemLog.LogLevel.Fatal, logger.Fatal}
	};

	public void Log(ILogMessage message)
	{
		LogMessage(message);
	}

	public async Task LogAsync(ILogMessage message, CancellationToken ct)
	{
		await Task.Run(() => { LogMessage(message); }, ct);
	}

	private void LogMessage(ILogMessage message)
	{
		var msg = message is NLogMessage ? "NLogMessage: " + message.Message : message.Message;
		logLevelToActionConverter[message.Level]?.Invoke(message.Exception, msg);
	}
}