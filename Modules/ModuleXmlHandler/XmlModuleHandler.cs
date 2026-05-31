using Qenex.QSuite.Specifications.ComponentSpecification;
using Qenex.QSuite.Drivers.Driver;
using System.Globalization;
using System.Reflection;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Modules.Module;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Scripting.PythonScript;
using Qenex.QSuite.Scripting.Script;
using Qenex.QSuite.Scripting.ScriptingEngine;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.ValuePresentation;
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.UnifModule;
using Qenex.QSuite.Variables.ValueConversion;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.ModuleXmlHandler;

public class XmlModuleHandler
{
    private ILogger? logger;
    private IList<PluginDetails> driversDetails;
    private IList<PluginDetails> protocolsDetails;
    
    public XmlModuleHandler(IList<PluginDetails> drvsDetails, IList<PluginDetails> protsDetails, ILogger? logger = null)
    {
        driversDetails = drvsDetails;
        protocolsDetails = protsDetails;
        this.logger = logger;
    }
    
    public T? CreateModule<T>(XmlModule xmlModule, IModuleFactory<T> factory, ScriptEngineSettings scriptEngineSettings, ILogger? logger) where T: class, IModuleBase
    {
        var module = factory.Create(scriptEngineSettings, logger);

        //var module = new T();
        // Module xml name must match with the module name
        if (module.Specification.Name != xmlModule.Name)
        {
            logger?.Log(LogLevel.Error, "Module Name does not match with the UnifiedModule name.");
            return null;
        }

        // Add module specification
        module.Specification.Label = xmlModule.Label;
        module.Specification.Description = xmlModule.Description;
        module.Specification.Version = new Version(xmlModule.Version);
        module.Specification.Author = xmlModule.Author;
        module.Specification.Company = xmlModule.Company;
        module.Specification.CreatedOn = xmlModule.CreationDate == default ? xmlModule.CreatedOn : xmlModule.CreationDate;
        module.Specification.Modified = xmlModule.Modified;
        
        // Add Events
        module.AddVarEvents(GetVarEvents(xmlModule.Events));
        
        // Add Conversions
        module.AddConversions(GetConversions(xmlModule.Conversions));

        // Add Presentations
        module.AddPresentations(GetPresentations(module.Conversions, xmlModule.Presentations));
        
        // Add scripts
        module.Scripting.AddScripts(GetScripts(xmlModule.Scripts));

        // Add variables
        module.AddVariables(GetVariables(module.Presentations, xmlModule.Variables, module.VarEvents));
        
        // Add project drivers structure - including protocols and variables, presentations, conversions
        module.AddDrivers(GetDrivers(driversDetails, protocolsDetails, module.Variables, module.VarEvents, module.Scripting, xmlModule));
        
        return module;
    }

    public XmlModule CreateXmlModule(IModuleBase module)
    {
        return new XmlModule
        {
            Name = module.Specification.Name,
            Label = module.Specification.Label,
            Description = module.Specification.Description,
            Version = module.Specification.Version?.ToString() ?? string.Empty,
            Author = module.Specification.Author ?? string.Empty,
            Company = module.Specification.Company ?? string.Empty,
            CreationDate = module.Specification.CreatedOn,
            Modified = module.Specification.Modified,
            DriverReferences = GetXmlDriverReferences(module.Drivers, module.Scripting),
            Drivers = GetXmlDrivers(module.Drivers),
            Protocols = GetXmlProtocols(module.Drivers.SelectMany(d => d.Protocols)),
            Presentations = GetXmlPresentations(module.Presentations),
            Conversions = GetXmlConversions(module.Conversions),
            Events = GetXmlVarEvents(module.VarEvents),
            Variables = GetXmlVariables(module.Variables),
            Scripts = GetXmlScripts(module.Scripting.Scripts)
        };
    }

    private List<XmlDriverReference> GetXmlDriverReferences(IEnumerable<IDriverBase> drivers, ScriptingContext scripting)
    {
        var xmlDriverReferences = new List<XmlDriverReference>();

        foreach (var driver in drivers)
        {
            xmlDriverReferences.Add(new XmlDriverReference
            {
                Ref = driver.Specification.Name,
                Label = driver.Label,
                IsEnabled = driver.IsEnabled,
                Settings = driver.RawSettings,
                EncryptedSettings = driver.RawEncryptedSettings,
                ProtocolReferences = GetXmlProtocolReferences(driver.Protocols, scripting)
            });
        }

        return xmlDriverReferences;
    }

    private List<XmlDriver> GetXmlDrivers(IEnumerable<IDriverBase> drivers)
    {
        return drivers
            .GroupBy(d => new { d.Specification.Name, Version = d.Specification.Version?.ToString() ?? string.Empty })
            .Select((group, index) => new XmlDriver
            {
                Id = group.First().Id != 0 ? group.First().Id : index + 1,
                Name = group.Key.Name,
                Version = group.Key.Version
            })
            .ToList();
    }

    private List<XmlProtocolReference> GetXmlProtocolReferences(IEnumerable<IProtocolBase> protocols, ScriptingContext scripting)
    {
        var xmlProtocolReferences = new List<XmlProtocolReference>();

        foreach (var protocol in protocols)
        {
            xmlProtocolReferences.Add(new XmlProtocolReference
            {
                Ref = protocol.Specification.Name,
                IsEnabled = protocol.IsEnabled,
                Settings = protocol.RawSettings,
                EncryptedSettings = protocol.RawEncryptedSettings,
                VariableReferences = GetXmlVariableReferences(protocol.Variables, scripting)
            });
        }

        return xmlProtocolReferences;
    }

    private List<XmlProtocol> GetXmlProtocols(IEnumerable<IProtocolBase> protocols)
    {
        return protocols
            .GroupBy(p => new
            {
                p.Specification.Name,
                p.Specification.Label,
                Version = p.Specification.Version?.ToString() ?? string.Empty
            })
            .Select((group, index) => new XmlProtocol
            {
                Id = group.First().Id != 0 ? group.First().Id : index + 1,
                Name = group.Key.Name,
                Label = group.Key.Label,
                Version = group.Key.Version
            })
            .ToList();
    }

    private List<XmlVariableReferenceBase> GetXmlVariableReferences(IEnumerable<IProtocolVariable> protocolVariables, ScriptingContext scripting)
    {
        return protocolVariables.Select(protocolVariable => new XmlVariableReference
        {
            Ref = protocolVariable.Variable.Id,
            IsCommunicated = protocolVariable.IsCommunicated,
            CommParam = GetCommParam(protocolVariable.ProtocolVariableSpecification),
            Scripts = scripting.GetOnValueChangedScriptTriggers(protocolVariable.Variable.Id)
                .Select(trigger => new XmlScriptReference
                {
                    Ref = trigger.ScriptFileName,
                    AdditionalInfo = trigger.AdditionalInfo
                })
                .ToList()
        }).Cast<XmlVariableReferenceBase>().ToList();
    }

    private List<XmlPresentation> GetXmlPresentations(IEnumerable<IPresentation> presentations)
    {
        return presentations.Select(presentation => new XmlPresentation
        {
            Name = presentation.Name,
            Label = presentation.Label,
            Min = presentation.Min,
            Max = presentation.Max,
            PrintFormat = presentation.PrintFormat,
            Unit = presentation.Unit,
            ConversionReference = new XmlConversionReference { Ref = presentation.Conversion.Name }
        }).ToList();
    }

    private List<XmlConversion> GetXmlConversions(IEnumerable<IValConversion> conversions)
    {
        var xmlConversions = new List<XmlConversion>();

        foreach (var conversion in conversions)
        {
            if (conversion is LinearValConversion linearConversion)
            {
                xmlConversions.Add(new XmlLinearConversion
                {
                    Name = linearConversion.Name,
                    Multiplier = linearConversion.Multiplier,
                    Offset = linearConversion.Offset
                });
            }
            else if (conversion is EnumValConversion enumConversion)
            {
                xmlConversions.Add(new XmlEnumConversion
                {
                    Name = enumConversion.Name,
                    Enums = enumConversion.Enums.Select(e => new XmlEnum
                    {
                        Name = e.Name,
                        Value = e.Value
                    }).ToList()
                });
            }
            else
            {
                logger?.Log(LogLevel.Warn, $"Conversion type {conversion.GetType()} is not supported.");
            }
        }

        return xmlConversions;
    }

    private List<XmlVarEvent> GetXmlVarEvents(IEnumerable<IVarEvent> varEvents)
    {
        var xmlVarEvents = new List<XmlVarEvent>();

        foreach (var varEvent in varEvents)
        {
            if (varEvent is PeriodicVarEvent periodicVarEvent)
            {
                xmlVarEvents.Add(new PeriodicXmlVarEvent
                {
                    Name = periodicVarEvent.Name,
                    Period = periodicVarEvent.Period,
                    Unit = periodicVarEvent.Unit.ToString()
                });
            }
            else if (varEvent is OnRequestVarEvent)
            {
                xmlVarEvents.Add(new OnRequestXmlVarEvent { Name = varEvent.Name });
            }
            else if (varEvent is OnValueChangedVarEvent onValueChangedVarEvent)
            {
                xmlVarEvents.Add(new OnValueChangedXmlVarEvent
                {
                    Name = onValueChangedVarEvent.Name,
                    Threshold = onValueChangedVarEvent.Threshold
                });
            }
            else
            {
                logger?.Log(LogLevel.Warn, $"Variable event type {varEvent.GetType()} is not supported.");
            }
        }

        return xmlVarEvents;
    }

    private List<XmlVariable> GetXmlVariables(IEnumerable<IVariableBase> variables)
    {
        var xmlVariables = new List<XmlVariable>();

        foreach (var variable in variables)
        {
            if (variable is ScalarVariable scalarVariable)
            {
                xmlVariables.Add(new XmlScalarVariable
                {
                    Id = scalarVariable.Id,
                    Namespace = scalarVariable.Namespace,
                    Name = scalarVariable.Name,
                    Label = scalarVariable.Label,
                    Description = scalarVariable.Description,
                    Size = scalarVariable.Size,
                    Values = new XmlValues
                    {
                        DataType = Enum.Parse<XmlValuesDataType>(scalarVariable.Values.ValueType.ToString()),
                        Size = scalarVariable.Values.Size,
                        Length = scalarVariable.Values.Length,
                        PresentationReference = new XmlPresentationReference
                        {
                            Ref = scalarVariable.Values.ValPresentation.Name
                        }
                    }
                });
            }
            else if (variable is StringVariable stringVariable)
            {
                xmlVariables.Add(new XmlStringVariable
                {
                    Id = stringVariable.Id,
                    Namespace = stringVariable.Namespace,
                    Name = stringVariable.Name,
                    Label = stringVariable.Label,
                    Description = stringVariable.Description
                });
            }
            else
            {
                logger?.Log(LogLevel.Warn, $"Variable type {variable.GetType()} is not supported.");
            }
        }

        return xmlVariables;
    }

    private List<XmlScript> GetXmlScripts(IEnumerable<IScriptBase> scripts)
    {
        var xmlScripts = new List<XmlScript>();

        foreach (var script in scripts)
        {
            if (!Enum.TryParse<XmlScriptExecutionMode>(script.ExecutionMode.ToString(), out var executionMode))
            {
                logger?.Log(LogLevel.Warn, $"Script execution mode {script.ExecutionMode} is not supported by XML module.");
                executionMode = XmlScriptExecutionMode.Manual;
            }

            xmlScripts.Add(new XmlScript
            {
                FileName = script.FileName,
                Content = script.Content,
                ExecutionMode = executionMode,
                Blocking = script.Blocking,
                TimeoutMs = script.TimeoutMs,
                AdditionalInfo = RemoveScriptExecutionOptions(script.AdditionalInfo),
                IsEnabled = script.IsEnabled,
                IsReplayEnabled = script.IsReplayEnabled
            });
        }

        return xmlScripts;
    }

    private string GetCommParam(IProtVariableSpecification specification)
    {
        var properties = specification.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead)
            .ToList();

        var parameters = new List<string>();
        AddCommParam(parameters, properties, specification, "Direction", value => value.ToString()!.ToLowerInvariant());
        AddCommParam(parameters, properties, specification, "VariableEvent", value => ((IVarEvent)value).Name, "eventRef");
        AddCommParam(parameters, properties, specification, "Multiplier");

        foreach (var property in properties.Where(p => p.Name is not ("Name" or "Direction" or "VariableEvent" or "Multiplier")))
        {
            var value = property.GetValue(specification);
            if (value == null)
            {
                continue;
            }

            parameters.Add($"{ToCamelCase(property.Name)}=\"{Convert.ToString(value, CultureInfo.InvariantCulture)}\"");
        }

        return string.Join(";", parameters);
    }

    private static void AddCommParam(
        ICollection<string> parameters,
        IEnumerable<PropertyInfo> properties,
        object specification,
        string propertyName,
        Func<object, string>? valueFormatter = null,
        string? parameterName = null)
    {
        var property = properties.FirstOrDefault(p => p.Name == propertyName);
        var value = property?.GetValue(specification);
        if (value == null)
        {
            return;
        }

        parameters.Add($"{parameterName ?? ToCamelCase(propertyName)}=\"{(valueFormatter?.Invoke(value) ?? Convert.ToString(value, CultureInfo.InvariantCulture))}\"");
    }

    private static string ToCamelCase(string value)
    {
        return string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
    }

    private List<IVariableBase> GetVariables(IEnumerable<IPresentation> presentations, IEnumerable<XmlVariable> xmlVariables, IEnumerable<IVarEvent> variableEvents)
    {
        var variables = new List<IVariableBase>();
        
        foreach (var xmlVariable in xmlVariables)
        {
            if (!XmlModuleGlobal.TypeOfXmlVariable2VariableEnumDict.TryGetValue(xmlVariable.GetType(), out var variableType))
            {
                logger?.Log(LogLevel.Warn, $"Variable type {xmlVariable.GetType()} is not supported.");
                continue;
            }

            try
            {
                var variable = VariablesGlobal.CreateInstance(variableType);
                variable.Id = xmlVariable.Id;
                variable.Namespace = xmlVariable.Namespace;
                variable.Name = xmlVariable.Name;
                variable.Label = xmlVariable.Label;
                variable.Description = xmlVariable.Description;

                if (variable is ScalarVariable scalarVariable && xmlVariable is XmlScalarVariable xmlScalarVariable)
                {
                    scalarVariable.Size = xmlScalarVariable.Size;
                    scalarVariable.Values = CreateScalarValues(presentations, xmlScalarVariable.Values);
                }
                
                variables.Add(variable);
            }
            catch (ArgumentException e)
            {
                logger?.Log(LogLevel.Error, $"Error creating variable {xmlVariable.Name}: {e.Message}.");
            }
            
        }

        return variables;
    }

    private List<IVarEvent> GetVarEvents(IEnumerable<XmlVarEvent> xmlEvents)
    {
        var varEvents = new List<IVarEvent>();

        foreach (var xmlEvent in xmlEvents)
        {
            if (!XmlModuleGlobal.TypeOfXmlEvent2EventEnumDict.TryGetValue(xmlEvent.GetType(), out var varEventType))
			{
				logger?.Log(LogLevel.Warn, $"Variable event type {xmlEvent.GetType()} is not supported.");
				continue;
			}

            try
            {
                var varEvent = EventsGlobal.CreateInstance(varEventType);
                varEvent.Name = xmlEvent.Name;

                if (varEvent is PeriodicVarEvent periodicVarEvent && xmlEvent is PeriodicXmlVarEvent xmlPeriodicEvent)
                {
                    periodicVarEvent.Period = xmlPeriodicEvent.Period;
                    periodicVarEvent.Unit = Enum.Parse<TimeUnit>(xmlPeriodicEvent.Unit);
                }
                else if (varEvent is OnRequestVarEvent onRequestVarEvent)
                {
                }
                else if (varEvent is OnValueChangedVarEvent onValueChangedVarEvent && xmlEvent is OnValueChangedXmlVarEvent xmlOnValueChangedEvent)
                {
                    onValueChangedVarEvent.Threshold = xmlOnValueChangedEvent.Threshold;
                }
                
                varEvents.Add(varEvent);
            }
            catch (ArgumentException e)
            {
                logger?.Log(LogLevel.Error, $"Error creating variable {xmlEvent.Name}: {e.Message}.");
            }
        }

        return varEvents;
    }

    private List<IValConversion> GetConversions(IEnumerable<XmlConversion> xmlConversions)
    {
        var conversions = new List<IValConversion>();
        
        foreach (var xmlConversion in xmlConversions)
        {
            if (!XmlModuleGlobal.TypeOfXmlConversion2ConversionEnumDict.TryGetValue(xmlConversion.GetType(), out var conversionType))
            {
                logger?.Log(LogLevel.Warn, $"Converion type {xmlConversion.GetType()} is not supported.");
                continue;
            }

            try
            {
                var conversion = ConversionsGlobal.CreateInstance(conversionType);
                conversion.Name = xmlConversion.Name;
                
                
                if (conversion is LinearValConversion lc && xmlConversion is XmlLinearConversion xmllc)
                {
                    lc.Multiplier = xmllc.Multiplier;
                    lc.Offset = xmllc.Offset;
                }
                else if (conversion is EnumValConversion ec && xmlConversion is XmlEnumConversion xmlec)
                {
                    ec.Enums = new List<EnumType>();
                    xmlec.Enums.ForEach(e => ec.Enums.Add(new EnumType() {Name = e.Name, Value = e.Value}));
                }
                
                conversions.Add(conversion);
            }
            catch (ArgumentException e)
            {
                logger?.Log(LogLevel.Error, $"Error creating conversion {xmlConversion.Name}: {e.Message}.");
            }
            
        }

        return conversions;
    }
    
    private List<IPresentation> GetPresentations(IList<IValConversion> conversions, IEnumerable<XmlPresentation> xmlPresentations)
    {
        var presentations = new List<IPresentation>();
        
        foreach (var xmlPresentation in xmlPresentations)
        {
            try
            {
                var presentation = new Presentation
                {
                    Name = xmlPresentation.Name,
                    Label = xmlPresentation.Label,
                    Min = xmlPresentation.Min,
                    Max = xmlPresentation.Max,
                    Unit = xmlPresentation.Unit,
                    Conversion = conversions.First(c => c.Name == xmlPresentation.ConversionReference.Ref)
                };
                
                presentations.Add(presentation);
            }
            catch (ArgumentException e)
            {
                logger?.Log(LogLevel.Error, $"Error creating presentation {xmlPresentation.Name}: {e.Message}.");
            }
            
        }
        return presentations;
    }
    
    private List<IScriptBase> GetScripts(IEnumerable<XmlScript> xmlPythonScripts) 
    {
        var scripts = new List<IScriptBase>();
        
        foreach (var xmlPythonScript in xmlPythonScripts)
        {
            var additionalInfo = ExtractScriptExecutionOptions(
                xmlPythonScript.AdditionalInfo,
                out var legacyBlocking,
                out var legacyTimeoutMs);

            var script = new PyScript
            {
                FileName = xmlPythonScript.FileName,
                Content = xmlPythonScript.Content,
				ExecutionMode = Enum.Parse<ScriptExecutionMode>(xmlPythonScript.ExecutionMode.ToString()),
                Blocking = legacyBlocking ?? xmlPythonScript.Blocking,
                TimeoutMs = legacyTimeoutMs ?? xmlPythonScript.TimeoutMs,
                AdditionalInfo = additionalInfo,
                IsEnabled = xmlPythonScript.IsEnabled,
                IsReplayEnabled = xmlPythonScript.IsReplayEnabled
			};

            scripts.Add(script);
        }
        
        return scripts;
    }

    private static string RemoveScriptExecutionOptions(string additionalInfo)
    {
        return ExtractScriptExecutionOptions(additionalInfo, out _, out _);
    }

    private static string ExtractScriptExecutionOptions(string additionalInfo, out bool? blocking, out double? timeoutMs)
    {
        blocking = null;
        timeoutMs = null;

        if (string.IsNullOrWhiteSpace(additionalInfo))
        {
            return string.Empty;
        }

        var parameters = new List<string>();
        var parts = additionalInfo.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (keyValue.Length != 2)
            {
                parameters.Add(part);
                continue;
            }

            if (keyValue[0].Equals("blocking", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseBoolean(keyValue[1], out var parsedBlocking))
                {
                    blocking = parsedBlocking;
                }

                continue;
            }

            if (keyValue[0].Equals("timeout", StringComparison.OrdinalIgnoreCase))
            {
                if (double.TryParse(keyValue[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedTimeoutMs) &&
                    parsedTimeoutMs >= 0)
                {
                    timeoutMs = parsedTimeoutMs;
                }

                continue;
            }

            parameters.Add(part);
        }

        return string.Join(";", parameters);
    }

    private static bool TryParseBoolean(string value, out bool result)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "true":
            case "1":
            case "yes":
            case "y":
                result = true;
                return true;
            case "false":
            case "0":
            case "no":
            case "n":
                result = false;
                return true;
            default:
                result = false;
                return false;
        }
    }

    private IValuesBase CreateScalarValues(IEnumerable<IPresentation> presentations, XmlValues xmlValues)
    {
        var values = ValuesGlobal.CreateInstance(Enum.Parse<ValuesGlobal.ValueDataType>(xmlValues.DataType.ToString()));
        values.ValPresentation = presentations.FirstOrDefault(p => p.Name == xmlValues.PresentationReference.Ref);
        if (values.ValPresentation == null)
        {
            logger?.Log(LogLevel.Error, $"Presentation \"{xmlValues.PresentationReference.Ref}\" not found.");
        }
        
        return values;
    }

    private List<IDriverBase> GetDrivers(IList<PluginDetails> driversDetails, IList<PluginDetails> protocolsdetails, IList<IVariableBase> variables, IList<IVarEvent> varEvents, ScriptingContext scripting, XmlModule xmlModule)
    {
        var tempDrivers = new List<IDriverBase>();
        
        var pluginManager = new PluginLoader(logger);
        
        foreach (var driverRef in xmlModule.DriverReferences)
        {
            // find xml driver based on reference in driver references
            var xmlDriver = xmlModule.Drivers.FirstOrDefault(d => d.Name == driverRef.Ref);
            if (xmlDriver == null)
            {
                logger?.Log(LogLevel.Error, $"The driver \"{driverRef.Ref}\" not found in the module file.");
                continue;
            }
            
            // xml driver found, now find the real driver 
            var xmlDriverVersion = new Version(xmlDriver.Version);
            var driverPath = driversDetails.FirstOrDefault(p => p.Name == xmlDriver.Name && p.Version.Major.Equals(xmlDriverVersion.Major) && p.Version.Minor.Equals(xmlDriverVersion.Minor));
            if (driverPath == null)
            {
                logger?.Log(LogLevel.Error, $"The driver \"{xmlDriver.Name}\" and version \"{xmlDriver.Version}\" not found in driver directory.");
                continue;
            }

            var driver = pluginManager.LoadPlugin<IDriverBase>(driverPath.PathName);
            
            if (driver == null)
            {
                logger?.Log(LogLevel.Error, $"The driver \"{xmlDriver.Name}\" could not be loaded.");
                continue;
            }
            
            driver.Label = driverRef.Label;
            driver.IsEnabled = driverRef.IsEnabled;
            driver.RawSettings = driverRef.Settings;
            driver.RawEncryptedSettings = driverRef.EncryptedSettings;
            driver.SetConfiguration();
            driver.AddProtocols(GetProtocols(protocolsdetails, variables, varEvents, scripting, driverRef.ProtocolReferences, xmlModule));
            
            
            tempDrivers.Add(driver);
        }
        
        return tempDrivers;
    }
    
    private IList<IProtocolBase> GetProtocols(IList<PluginDetails> protocolsDetails, IList<IVariableBase> variables, IList<IVarEvent> varEvents, ScriptingContext scripting, IList<XmlProtocolReference> xmlProtocolReferences, XmlModule xmlModule)
    {
        var tempProtocols = new List<IProtocolBase>();
        
        var pluginManager = new PluginLoader(logger);
        
        foreach (var protocolRef in xmlProtocolReferences)
        {
            // find xml protocol based on reference in protocol references
            var xmlProtocol = xmlModule.Protocols.FirstOrDefault(p => p.Name == protocolRef.Ref);
            if (xmlProtocol == null)
            {
                logger?.Log(LogLevel.Error, $"The protocol \"{protocolRef.Ref}\" not found in the module file.");
                continue;
            }
            var xmlProtocolVersion = new Version(xmlProtocol.Version);
            var protocolDetails = protocolsDetails.FirstOrDefault(p => p.Name == xmlProtocol.Name && p.Version.Major.Equals(xmlProtocolVersion.Major) && p.Version.Minor.Equals(xmlProtocolVersion.Minor));
            if (protocolDetails == null)
            {
                logger?.Log(LogLevel.Error, $"The protocol \"{xmlProtocol.Name}\" not found among loaded protocols.");
                continue;
            }
            
            var protocol = pluginManager.LoadPlugin<IProtocolBase>(protocolDetails.PathName);
            
            if (protocol == null)
            {
                logger?.Log(LogLevel.Error, $"The protocol \"{xmlProtocol.Name}\" v. \"{xmlProtocol.Version}\" could not be loaded.");
                continue;
            }

            protocol.RawSettings = protocolRef.Settings;
            protocol.RawEncryptedSettings = protocolRef.EncryptedSettings;
            foreach (var variableRef in protocolRef.VariableReferences)
            {
                var variable = variables.FirstOrDefault(v => v.Id == variableRef.Ref);
                if (variable == null)
                {
                    logger?.Log(LogLevel.Error, $"the variable \"{variableRef.Ref}\" not found.");
                    continue;
                }

                var eventRefName = variableRef.CommParam.Split(';').FirstOrDefault(e => e.Contains("eventRef"));
                
                var protocolVariable = string.IsNullOrEmpty(eventRefName) ? 
                    protocol.CreateProtocolVariable(variable, variableRef.CommParam, variableRef.IsCommunicated) : 
                    protocol.CreateProtocolVariable(variable, varEvents, variableRef.CommParam, variableRef.IsCommunicated);
                
                if (protocolVariable == null)
                {
                    logger?.Log(LogLevel.Error, $"The protocol variable for \"{variable.Name}\" could not be created.");
                    continue;
                }
                
                protocolVariable.IsCommunicated = variableRef.IsCommunicated;
                protocol.AddVariable(protocolVariable);

                foreach (var scriptRef in variableRef.Scripts)
                {
                    scripting.AddOnValueChangedScriptTrigger(variable.Id, scriptRef.Ref, scriptRef.AdditionalInfo);
                }
            }
            
            protocol.IsEnabled = protocolRef.IsEnabled;
            protocol.SetConfiguration();
            tempProtocols.Add(protocol);
        }
        
        return tempProtocols;
    }
}
