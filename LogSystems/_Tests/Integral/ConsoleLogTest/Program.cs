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
        Logger logger = new Logger();
        logger.RegisterSubscriber(new ConsoleSubscriber());

        var logMessage = new ConsoleMessage(LogLevel.Info, "Hello, World!");
        logger.Log(logMessage);
            
        Console.WriteLine("End!");
    }
}