using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.Driver;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Module;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.PluginManager;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.QVariables;
using Qenex.QSuite.ValuePresentation;
using Qenex.QSuite.QVariables.Values;
using Qenex.QSuite.UnifModule;
using Qenex.QSuite.ValueConversion;
using Qenex.QSuite.VariableEvents;

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
    
    public T? CreateModule<T>(XmlModule xmlModule) where T: class, IModuleBase, new()
    {
        var module = new T();
        
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
        module.Specification.CreatedOn = xmlModule.CreatedOn;
        
        // Add Events
        module.AddVarEvents(GetVarEvents(xmlModule.Events));
        
        // Add Conversions
        module.AddConversions(GetConversions(xmlModule.Conversions));

        // Add Presentations
        module.AddPresentations(GetPresentations(module.Conversions, xmlModule.Presentations));

        // Add variables
        module.AddVariables(GetVariables(module.Presentations, xmlModule.Variables, module.VarEvents));
        
        // Add project drivers structure - including protocols and variables, presentations, conversions
        module.AddDrivers(GetDrivers(driversDetails, protocolsDetails, module.Variables, module.VarEvents, xmlModule));
        
        return module;
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

    private List<IDriverBase> GetDrivers(IList<PluginDetails> driversDetails, IList<PluginDetails> protocolsdetails, IList<IVariableBase> variables, IList<IVarEvent> varEvents, XmlModule xmlModule)
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
            var driverPath = driversDetails.FirstOrDefault(p => p.Name == xmlDriver.Name && p.Version.Equals(new Version(xmlDriver.Version)));
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
            driver.SetConfiguration(driverRef.Settings, driverRef.EncryptedSettings);
            driver.AddProtocols(GetProtocols(protocolsdetails, variables, varEvents, driverRef.ProtocolReferences, xmlModule));
            
            
            tempDrivers.Add(driver);
        }
        
        return tempDrivers;
    }
    
    private IList<IProtocolBase> GetProtocols(IList<PluginDetails> protocolsDetails, IList<IVariableBase> variables, IList<IVarEvent> varEvents, IList<XmlProtocolReference> xmlProtocolReferences, XmlModule xmlModule)
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
            
            var protocolDetails = protocolsDetails.FirstOrDefault(p => p.Name == xmlProtocol.Name && p.Version.Equals(new Version(xmlProtocol.Version)));
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

            foreach (var variableRef in protocolRef.VariableReferences)
            {
                var variable = variables.FirstOrDefault(v => v.Id == variableRef.Ref);
                if (variable == null)
                {
                    logger?.Log(LogLevel.Error, $"the variable \"{variableRef.Ref}\" not found.");
                    continue;
                }

                AddEventToVariable(variable, varEvents, variableRef.CommParam);
                var protocolVariable = protocol.CreateProtocolVariable(variable, variableRef.CommParam);
                protocol.AddVariable(protocolVariable);
            }
            
            tempProtocols.Add(protocol);
        }
        
        return tempProtocols;
    }
    
    private void AddEventToVariable(IVariableBase variable, IEnumerable<IVarEvent> varEvents, string commParam)
    {
        var eventRefName = commParam.Split(';').FirstOrDefault(e => e.Contains("eventRef"));
        if (string.IsNullOrEmpty(eventRefName)) return;
        eventRefName = eventRefName?.Split('=')[1].Trim('"');
        
        var varEvent = varEvents.FirstOrDefault(e => e.Name == eventRefName);
        if (varEvent == null) return;
        
        variable.Event = varEvent;
    }
}