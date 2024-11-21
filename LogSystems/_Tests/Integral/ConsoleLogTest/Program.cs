using Qenex.QSuite.ConsoleLogSubscriber;
using Qenex.QSuite.LogSystem;

namespace ConsoleLogTest;

/// <summary>
/// Program Test simple console log.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Logger logger = new Logger(LogLevel.Debug);
        logger.RegisterSubscriber(new ConsoleSubscriber());
        
        logger.Log(new LogMessage(LogLevel.Debug, "Debug msg"));
        logger.Log(new LogMessage(LogLevel.Info, "Info msg"));
        logger.Log(new LogMessage(LogLevel.Warn, "Warning msg"));
        logger.Log(new LogMessage(LogLevel.Error, "Error msg"));
        logger.Log(new LogMessage(LogLevel.Fatal, "Fatal msg"));
            
        Console.WriteLine("End!");
    }
}