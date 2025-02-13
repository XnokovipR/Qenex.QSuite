using System.Text.Json;
using System.Threading.Channels;
using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.ConsoleLogSubscriber;
using Qenex.QSuite.Drivers;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.ModuleXmlHandler;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.UnifModule;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.QVariables;

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

        foreach (var drv in realModule.Drivers)
        {
            drv.OnDataReceived += (sender, data) =>
            {
                if (data is not DataReceivedEventArgs<IEnumerable<IProtocolVariable>> pVars) return;
                foreach (var v in pVars.Data)
                {
                    if (v.Variable is ScalarVariable sv)
                    {
                        Console.WriteLine($"{pVars.Timestamp:HH:mm:ss.fff}\t{sv.Label}\t\t{sv.Values.ToString()}");
                    }
                }

                Console.WriteLine("-------------------------------------------------");
                
            };
        }


        var simDataDriver = realModule?.Drivers.First(d => d.Specification.Name == "SimulDataDriver");
        if (simDataDriver == null) return;
        
        simDataDriver.StartAsync(cts.Token);

        Console.WriteLine("Press ESC to exit");
        while (Console.ReadKey().Key != ConsoleKey.Q) { };
        
        // Cancel the driver
        //cts.Cancel();
        
        // Correct way to stop the driver
        simDataDriver.StopAsync(cts.Token);
    }
}