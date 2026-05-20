using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.Helpers.ProjectFile;
using Qenex.QSuite.ModuleXmlHandler;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Scripting.ScriptingEngine;
using Qenex.QSuite.UnifModule;

namespace Qenex.QSuite.Modules.Tests.XmlReadWriteTest;

class Program
{
    static async Task Main(string[] args)
    {
        
        try
        {
            
            // Deserialize settings from file
            var xmlModule = XmlInOut<XmlModule>.LoadFromFile(FromOutputDir(@"..\..\..\..\..\..\ModuleXmlHandler\Docs\XmlModule.xml"));
            
            // Serialize settings to file
            XmlInOut<XmlModule>.SaveToFile(FromOutputDir(@"..\..\..\..\..\..\ModuleXmlHandler\Docs\XmlModule_out.xml"), xmlModule);
            
            var pluginManager = new PluginLoader();
            var driversDetails = pluginManager.GetPluginDetails<IDriverBase>(FromOutputDir(@"..\..\..\..\..\..\..\Drivers\SimDataDriver\bin\Debug\net10.0"));
            var protocolsDetails = pluginManager.GetPluginDetails<IProtocolBase>(FromOutputDir(@"..\..\..\..\..\..\..\Protocols\SimpleProtocol\bin\Debug\net10.0"));
            
            var xmlModuleHandler = new XmlModuleHandler(driversDetails, protocolsDetails);
            var realModule = xmlModuleHandler.CreateModule<UnifiedModule>(xmlModule, new UnifiedModuleFactory(), new ScriptEngineSettings(), null);
            if (realModule == null)
            {
                Console.WriteLine("Real module was not created.");
                return;
            }

            var xmlModuleFromRealModule = xmlModuleHandler.CreateXmlModule(realModule);
            XmlInOut<XmlModule>.SaveToFile(FromOutputDir(@"..\..\..\..\..\..\ModuleXmlHandler\Docs\XmlModule_from_real.xml"), xmlModuleFromRealModule);

            var projectZip = new ProjectZip();
            await projectZip.ZipModuleAsync("ModuleTestProject.zip", realModule);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

        }
       

    }

    private static string FromOutputDir(string path)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
