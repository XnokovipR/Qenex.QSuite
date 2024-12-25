using System.Text.Json;
using System.Threading.Channels;
using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.ConsoleLogSubscriber;
using Qenex.QSuite.Driver;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.ModuleXmlHandler;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.PluginManager;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.UnifModule;
using Qenex.QSuite.Common.CoreComm;

namespace Qenex.QSuite.CreateRealModuleTest;

class Program
{
    static void Main(string[] args)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        
        Logger logger = new Logger(LogLevel.Trace);
        logger.RegisterSubscriber(new ConsoleSubscriber());
        
        //Load drivers and protocols
        var pluginManager = new PluginLoader(logger);
        var driversDetails = pluginManager.GetPluginDetails<IDriverBase>("./Drivers");
        var protocolsDetails = pluginManager.GetPluginDetails<IProtocolBase>("./Protocols");
        
        var xmlModule = XmlInOut<XmlModule>.LoadFromFile(@"..\..\..\..\..\..\ModuleXmlHandler\Docs\ModuleTest.xml");
        
        var xmlModuleHandler = new XmlModuleHandler(driversDetails, protocolsDetails, logger);
        var realModule = xmlModuleHandler.CreateModule<UnifiedModule>(xmlModule);

        var zmqDriver = realModule?.Drivers.First(d => d.Specification.Name == "ZeroMQ Client Driver");
        if (zmqDriver == null) return;
        
        zmqDriver.StartAsync(cts.Token);

        Console.WriteLine("Press ESC to exit");
        Console.ReadKey();
        
        // Cancel the driver
        //cts.Cancel();
        
        // Correct way to stop the driver
        zmqDriver.StopAsync(cts.Token);
        
        Console.WriteLine("Cancelled");
        Console.ReadKey();
    }
}