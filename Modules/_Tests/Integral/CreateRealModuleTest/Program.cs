using System.Text.Json;
using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.ConsoleLogSubscriber;
using Qenex.QSuite.Driver;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.ModuleXmlHandler;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.PluginManager;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.UnifModule;

namespace Qenex.QSuite.CreateRealModuleTest;

class Program
{
    static void Main(string[] args)
    {
        Logger logger = new Logger(LogLevel.Trace);
        logger.RegisterSubscriber(new ConsoleSubscriber());
        
        //Load drivers and protocols
        var pluginManager = new PluginLoader(logger);
        var drivers = pluginManager.LoadPlugins<IDriverBase>("./Drivers");
        var protocols = pluginManager.LoadPlugins<IProtocolBase>("./Protocols");
        
        var xmlModule = XmlInOut<XmlModule>.LoadFromFile(@"..\..\..\..\..\..\ModuleXmlHandler\Docs\ModuleTest.xml");
        
        var xmlModuleHandler = new XmlModuleHandler(drivers, protocols, logger);
        var realModule = xmlModuleHandler.CreateModule<UnifiedModule>(xmlModule);
        
    }
}