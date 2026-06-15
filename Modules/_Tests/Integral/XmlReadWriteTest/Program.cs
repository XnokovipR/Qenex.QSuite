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
using Qenex.QSuite.Variables.ValueConversion;
using Qenex.QSuite.Variables.ValuePresentation;
using System.Xml.Serialization;

namespace Qenex.QSuite.Modules.Tests.XmlReadWriteTest;

class Program
{
    static async Task Main(string[] args)
    {
        
        try
        {
            await VerifyOnValueChangedScriptTriggerAsync();
            await VerifyOnValueChangedTriggerModesAsync();
            VerifyVariableScriptReferenceXmlRoundTrip();
            VerifyScriptExecutionPropertiesXmlRoundTrip();
            
            // Deserialize settings from file
            var xmlModule = XmlInOut<XmlModule>.LoadFromFile(FromOutputDir(@"..\..\..\..\..\..\ModuleXmlHandler\Docs\XmlModule.xml"));
            
            // Serialize settings to file
            XmlInOut<XmlModule>.SaveToFile(FromOutputDir(@"..\..\..\..\..\..\ModuleXmlHandler\Docs\XmlModule_out.xml"), xmlModule);
            
            var pluginManager = new PluginLoader();
            var driversDetails = pluginManager.GetPluginDetails<IDriverBase>(FromOutputDir(@"..\..\..\..\..\..\..\Drivers\SimDataDriver\bin\Debug\net10.0"));
            var protocolsDetails = pluginManager.GetPluginDetails<IProtocolBase>(FromOutputDir(@"..\..\..\..\..\..\..\Protocols\SimulDataProtocol\bin\Debug\net10.0"));
            
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
            ExecutionMode = ScriptExecutionMode.OnValueChanged,
            IsEnabled = true
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

    private static async Task VerifyOnValueChangedTriggerModesAsync()
    {
        // Above: hranove (edge) nad RAW. Threshold 50.
        {
            var (context, script, count) = CreateOnValueChangedContext();
            context.AddOnValueChangedScriptTrigger(1, script.FileName, "mode=above;threshold=50");
            var variable = new ScalarVariable { Id = 1, Name = "P", Values = new Values<int> { Value = 40 } };

            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 0, "above: first sample only seeds the state.");

            variable.SetValue(60);
            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 1, "above: crossing the threshold upwards fires once.");

            variable.SetValue(70);
            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 1, "above: staying above must not refire (edge).");

            variable.SetValue(30);
            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 1, "above: dropping below must not fire.");

            variable.SetValue(55);
            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 2, "above: re-crossing fires again.");
        }

        // Below: hranove nad RAW. Threshold 10.
        {
            var (context, script, count) = CreateOnValueChangedContext();
            context.AddOnValueChangedScriptTrigger(1, script.FileName, "mode=below;threshold=10");
            var variable = new ScalarVariable { Id = 1, Name = "P", Values = new Values<int> { Value = 40 } };

            await context.HandleVariableValueChangedAsync(variable);
            variable.SetValue(5);
            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 1, "below: crossing the threshold downwards fires once.");

            variable.SetValue(3);
            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 1, "below: staying below must not refire (edge).");

            variable.SetValue(20);
            await context.HandleVariableValueChangedAsync(variable);
            variable.SetValue(8);
            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 2, "below: re-crossing fires again.");
        }

        // Above s hysterezi: threshold 50, hysteresis 10 -> vypina az pod 40.
        {
            var (context, script, count) = CreateOnValueChangedContext();
            context.AddOnValueChangedScriptTrigger(1, script.FileName, "mode=above;threshold=50;hysteresis=10");
            var variable = new ScalarVariable { Id = 1, Name = "P", Values = new Values<int> { Value = 40 } };

            await context.HandleVariableValueChangedAsync(variable);
            variable.SetValue(60);
            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 1, "hysteresis: crossing above fires once.");

            variable.SetValue(45);
            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 1, "hysteresis: dip to 45 (above T-H=40) stays active, no refire.");

            variable.SetValue(55);
            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 1, "hysteresis: bounce back to 55 must not refire (still armed).");

            variable.SetValue(38);
            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 1, "hysteresis: dropping below T-H=40 releases without firing.");

            variable.SetValue(52);
            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 2, "hysteresis: re-crossing above after release fires again.");
        }

        // Eng source: Linear konverze eng = raw + 5000; mez nad ENG (raw zustava maly).
        {
            var (context, script, count) = CreateOnValueChangedContext();
            context.AddOnValueChangedScriptTrigger(1, script.FileName, "mode=above;threshold=5050;source=eng");
            var variable = new ScalarVariable
            {
                Id = 1,
                Name = "P",
                Values = new Values<int>
                {
                    Value = 10,
                    ValPresentation = new Presentation
                    {
                        Conversion = new LinearValConversion { Multiplier = 1, Offset = 5000 }
                    }
                }
            };

            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 0, "eng above: eng 5010 below threshold seeds only.");

            variable.SetValue(60);
            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 1, "eng above: eng 5060 crosses 5050 (raw 60 alone would not).");
        }

        // Vice triggeru na jedne promenne: below 10 a soucasne above 55.
        {
            var (context, script, count) = CreateOnValueChangedContext();
            context.AddOnValueChangedScriptTrigger(1, script.FileName, "mode=below;threshold=10");
            context.AddOnValueChangedScriptTrigger(1, script.FileName, "mode=above;threshold=55");
            var variable = new ScalarVariable { Id = 1, Name = "P", Values = new Values<int> { Value = 30 } };

            await context.HandleVariableValueChangedAsync(variable);
            variable.SetValue(5);
            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 1, "multi: below trigger fires.");

            variable.SetValue(60);
            await context.HandleVariableValueChangedAsync(variable);
            Assert(count() == 2, "multi: above trigger fires independently.");
        }
    }

    private static (ScriptingContext Context, IScriptBase Script, Func<int> ExecutedCount) CreateOnValueChangedContext()
    {
        var context = new ScriptingContext(new ScriptEngineSettings());
        var script = new Qenex.QSuite.Scripting.PythonScript.PyScript
        {
            FileName = "trigger.py",
            ExecutionMode = ScriptExecutionMode.OnValueChanged,
            IsEnabled = true
        };
        context.AddScript(script);

        var executedCount = 0;
        context.ScriptExecuted += (_, e) =>
        {
            if (ReferenceEquals(e.Script, script))
            {
                executedCount++;
            }
        };

        return (context, script, () => executedCount);
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

    private static void VerifyScriptExecutionPropertiesXmlRoundTrip()
    {
        var script = new XmlScript
        {
            FileName = "startup.py",
            ExecutionMode = XmlScriptExecutionMode.Startup,
            Blocking = false,
            TimeoutMs = 5000,
            AdditionalInfo = string.Empty,
            IsEnabled = true,
            IsReplayEnabled = false
        };

        var serializer = new XmlSerializer(typeof(XmlScript));
        using var writer = new StringWriter();
        serializer.Serialize(writer, script);

        var xml = writer.ToString();
        Assert(xml.Contains("blocking=\"false\""), "Script blocking was not serialized as an independent attribute.");
        Assert(xml.Contains("timeout=\"5000\""), "Script timeout was not serialized as an independent attribute.");

        using var reader = new StringReader(xml);
        var deserialized = (XmlScript)serializer.Deserialize(reader)!;
        Assert(!deserialized.Blocking, "Script blocking did not round-trip.");
        Assert(Math.Abs(deserialized.TimeoutMs - 5000) < 1e-9, "Script timeout did not round-trip.");
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
