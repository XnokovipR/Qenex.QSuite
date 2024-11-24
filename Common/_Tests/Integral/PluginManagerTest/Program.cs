using Qenex.QSuite.ConsoleLogSubscriber;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.PluginBase;
using Qenex.QSuite.PluginManager;

namespace Qenex.QSuite.PluginManagerTest;

class Program
{
    static void Main(string[] args)
    {
        Logger logger = new Logger(LogLevel.Trace);
        logger.RegisterSubscriber(new ConsoleSubscriber());
        
        
        Console.WriteLine("Read plugins");
        var pluginManager = new PluginLoader(logger);
        var plugins = pluginManager.LoadPlugins<IPlugin>("./Plugins");
        
        foreach (var plugin in plugins)
        {
            plugin.DisplayInfo();
        }
    }
}