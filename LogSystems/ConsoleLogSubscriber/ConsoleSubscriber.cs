using System.Diagnostics;
using System.Text;
using Qenex.CmLibs.DebugFeatures;
using Qenex.QSuite.LogSystem;

namespace Qenex.QSuite.ConsoleLogSubscriber;

public class ConsoleSubscriber : ILogSubscriber
{
	private static readonly Dictionary<LogLevel, ConsoleColor> levelToColorConverter = new()
	{
		[LogLevel.Debug] = ConsoleColor.Green,
		[LogLevel.Trace] = ConsoleColor.Cyan,
		[LogLevel.Info] = ConsoleColor.Gray,
		[LogLevel.Warn] = ConsoleColor.Yellow,
		[LogLevel.Error] = ConsoleColor.Red,
		[LogLevel.Fatal] = ConsoleColor.Magenta,
	};

	public void Log(ILogMessage message)
	{
		var msg = new StringBuilder($"{message.Timestamp:HH:mm:ss.ffff}");
		msg.Append($"\t{message.Level}\t{message.Message}");
		if (message.Exception != null)
		{
			msg.Append($" ({message.Exception.Message})");
		}
		ConsoleHelper.ColoredWriteLine(levelToColorConverter[message.Level], ConsoleColor.Black, 
			msg.ToString());
	}

	public Task LogAsync(ILogMessage message, CancellationToken ct = default)
	{
		throw new NotSupportedException("ConsoleSubscriber does not support async logging.");
	}
}