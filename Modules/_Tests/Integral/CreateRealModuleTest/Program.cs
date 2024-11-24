using System.Text.Json;
using Qenex.QSuite.ConsoleLogSubscriber;
using Qenex.QSuite.Driver;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.ModuleJsonHandler;
using Qenex.QSuite.ModuleJsonHandler.JsonModuleConverters;
using Qenex.QSuite.ModuleJsonHandler.JsonStructure;
using Qenex.QSuite.PluginManager;
using Qenex.QSuite.Protocol;

namespace CreateRealModuleTest;

class Program
{
    static void Main(string[] args)
    {
        Logger logger = new Logger(LogLevel.Trace);
        logger.RegisterSubscriber(new ConsoleSubscriber());
        
        // Load drivers and protocols
        var pluginManager = new PluginLoader(logger);
        var drivers = pluginManager.LoadPlugins<IDriverBase>("./Drivers");
        var protocols = pluginManager.LoadPlugins<IProtocolBase>("./Protocols");
        
        // Load json file
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new VariableTypeConverter()
            },
            WriteIndented = true
        };
        
        // Deserialize settings from file
        var json = File.ReadAllText("ModuleTest.json");
        var jsonModule = JsonSerializer.Deserialize<JsonModule>(json, options);
        
        // Create real module
        var jsonModuleHandler = new JsonModuleHandler(drivers, protocols, logger);
        var realModule = jsonModuleHandler.GetModule(jsonModule);
    }
}