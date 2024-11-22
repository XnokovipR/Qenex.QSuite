using System.Text.Json;
using Qenex.QSuite.Module;
using Qenex.QSuite.ModuleJsonHandler.JsonModuleConverters;
using Qenex.QSuite.UnifModule;

namespace JsonSerializationTest;

class Program
{
    static void Main(string[] args)
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new VariableTypeConverter(), new SpecificationConverter() },
            WriteIndented = true
        };
        
        // Deserialize settings from file
        var json = File.ReadAllText("ModuleTest.json");
        var moduleSettings = JsonSerializer.Deserialize<UnifiedModule>(json, options);
        
        // Serialize settings to file
        var jsonModuleSettings = JsonSerializer.Serialize(moduleSettings, options);
        File.WriteAllText("ModuleTest_Output.json", jsonModuleSettings);
        
        // Deserialize settings from newly written file
        var json2 = File.ReadAllText("ModuleTest_Output.json");
        var moduleSettings2 = JsonSerializer.Deserialize<UnifiedModule>(json, options);
    }
}