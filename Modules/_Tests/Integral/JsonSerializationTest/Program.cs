using System.Text.Json;
using Qenex.QSuite.ModuleJsonHandler.JsonModuleConverters;
using Qenex.QSuite.ModuleJsonHandler.JsonModuleStructure;

namespace JsonSerializationTest;

class Program
{
    static void Main(string[] args)
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new VariableTypeConverter() },
            WriteIndented = true
        };
        
        string filePath = "ModuleTest.json";
        var json = File.ReadAllText(filePath);
        var moduleSettings = JsonSerializer.Deserialize<ModuleSettings>(json, options);
    }
}