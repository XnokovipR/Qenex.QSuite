using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.Helpers.ProjectFile;
using Qenex.QSuite.ModuleXmlHandler;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Scripting.Script;
using Qenex.QSuite.Scripting.ScriptingEngine;
using Qenex.QSuite.UnifModule;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using System.Xml.Serialization;

namespace Qenex.QSuite.Modules.Tests.XmlReadWriteTest;

class Program
{
    static async Task Main(string[] args)
    {
        
        try
        {
            await VerifyOnValueChangedScriptTriggerAsync();
            VerifyVariableScriptReferenceXmlRoundTrip();
            
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
            Environment.ExitCode = 1;

        }
       

    }

    private static async Task VerifyOnValueChangedScriptTriggerAsync()
    {
        var context = new ScriptingContext(new ScriptEngineSettings());
        var script = new Qenex.QSuite.Scripting.PythonScript.PyScript
        {
            FileName = "oiltemp_valuechanged.py",
            ExecutionMode = ScriptExecutionMode.OnValueChanged
        };

        var executedCount = 0;
        context.AddScript(script);
        context.AddOnValueChangedScriptTrigger(1, script.FileName, "threshold=5");
        context.ScriptExecuted += (_, e) =>
        {
            if (ReferenceEquals(e.Script, script))
            {
                executedCount++;
            }
        };

        var variable = new ScalarVariable
        {
            Id = 1,
            Name = "OilTemp",
            Values = new Values<int> { Value = 10 }
        };

        await context.HandleVariableValueChangedAsync(variable);
        Assert(executedCount == 0, "First OnValueChanged value must only initialize the baseline.");

        variable.SetValue(12);
        await context.HandleVariableValueChangedAsync(variable);
        Assert(executedCount == 0, "OnValueChanged delta below threshold must not execute the script.");

        variable.SetValue(18);
        await context.HandleVariableValueChangedAsync(variable);
        Assert(executedCount == 1, "OnValueChanged delta above threshold must execute the script once.");

        var stringVariable = new StringVariable
        {
            Id = 2,
            Name = "AlertMsg",
            Values = "warning"
        };

        context.AddOnValueChangedScriptTrigger(2, script.FileName, "threshold=0");
        await context.HandleVariableValueChangedAsync(stringVariable);
        Assert(executedCount == 1, "Non-numeric OnValueChanged values must be skipped.");
    }

    private static void VerifyVariableScriptReferenceXmlRoundTrip()
    {
        var variableReference = new XmlVariableReference
        {
            Ref = 1,
            IsCommunicated = true,
            CommParam = "direction=\"read\"",
            Scripts =
            [
                new XmlScriptReference
                {
                    Ref = "oiltemp_valuechanged.py",
                    AdditionalInfo = "threshold=5.2"
                }
            ]
        };

        var serializer = new XmlSerializer(typeof(XmlVariableReference));
        using var writer = new StringWriter();
        serializer.Serialize(writer, variableReference);

        var xml = writer.ToString();
        Assert(xml.Contains("<script ref=\"oiltemp_valuechanged.py\" additionalInfo=\"threshold=5.2\""), "Nested script reference was not serialized.");

        using var reader = new StringReader(xml);
        var deserialized = (XmlVariableReference)serializer.Deserialize(reader)!;
        Assert(deserialized.Scripts.Count == 1, "Nested script reference was not deserialized.");
        Assert(deserialized.Scripts[0].Ref == "oiltemp_valuechanged.py", "Nested script reference ref did not round-trip.");
        Assert(deserialized.Scripts[0].AdditionalInfo == "threshold=5.2", "Nested script reference additionalInfo did not round-trip.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static string FromOutputDir(string path)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
