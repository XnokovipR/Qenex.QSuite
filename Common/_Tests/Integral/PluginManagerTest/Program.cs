using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.LogSystems.ConsoleLogSubscriber;
using Qenex.QSuite.Drivers;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QSuite.Common.Tests.PluginManagerTest;

class Program
{
    static void Main(string[] args)
    {
        Logger logger = new Logger(LogLevel.Trace);
        logger.RegisterSubscriber(new ConsoleSubscriber());
        
        
        Console.WriteLine("Read plugins");
        var pluginManager = new PluginLoader(logger);
        var pluginsInfo = pluginManager.GetPluginDetails<IDriverBase>("./Plugins");
        
        var pluginLoaded = pluginManager.LoadPlugin<IDriverBase>(pluginsInfo[0].PathName);
    }
}