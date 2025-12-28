using System.Text.Json;
using System.Threading.Channels;
using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.LogSystems.ConsoleLogSubscriber;
using Qenex.QSuite.Drivers;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.ModuleXmlHandler;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.UnifModule;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Modules.Tests.CreateRealModuleTest;

class Program
{
    static async Task Main(string[] args)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        
        Logger logger = new Logger(LogLevel.Trace);
        logger.RegisterSubscriber(new ConsoleSubscriber());
        
        //Load drivers and protocols
        var pluginManager = new PluginLoader(logger);
        var driversDetails = pluginManager.GetPluginDetails<IDriverBase>(@"..\..\..\..\..\..\..\Drivers\SimDataDriver\bin\Debug\net10.0");
        var protocolsDetails = pluginManager.GetPluginDetails<IProtocolBase>(@"..\..\..\..\..\..\..\Protocols\SimpleProtocol\bin\Debug\net10.0");
        
        var xmlModule = XmlInOut<XmlModule>.LoadFromFile(@"..\..\..\..\..\..\ModuleXmlHandler\Docs\XmlModule.xml");
        
        var xmlModuleHandler = new XmlModuleHandler(driversDetails, protocolsDetails, logger);
        var realModule = xmlModuleHandler.CreateModule<UnifiedModule>(xmlModule);

        foreach (var driver in realModule.Drivers)
        {
            foreach (var protocol in driver.Protocols)
            {
                foreach (var protocolVariable in protocol.Variables)
                {
                    protocolVariable.SubscribeAsyncValueChanged(async protVar =>
                    {
                        if (protocolVariable.Variable is ScalarVariable sv)
                        {
                            Console.WriteLine($"Variable {sv.Label} changed to {sv.Values}");
                        }
                    });
                }
            }
        }
        

        var simDataDriver = realModule?.Drivers.First(d => d.Specification.Name == "SimulDataDriver");
        if (simDataDriver == null) return;
        
        _ = simDataDriver.StartAsync(cts.Token);

        Console.WriteLine("Press ESC to exit");
        while (Console.ReadKey().Key != ConsoleKey.Q) { };
        
        // Cancel the driver
        //cts.Cancel();
        
        // Correct way to stop the driver
        await simDataDriver.StopAsync(cts.Token);
    }
}