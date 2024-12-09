using Qenex.QSuite.Driver;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Module;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.QVariables;
using Qenex.QSuite.ValuePresentation;
using Qenex.QSuite.QVariables.Values;
using Qenex.QSuite.UnifModule;
using Qenex.QSuite.ValueConversion;

namespace Qenex.QSuite.ModuleXmlHandler;

public class XmlModuleHandler
{
    private ILogger? logger;
    private IList<IDriverBase> drivers;
    private IList<IProtocolBase> protocols;
    
    public XmlModuleHandler(IList<IDriverBase> loadedDrivers, IList<IProtocolBase> loadedProtocols, ILogger? logger = null)
    {
        drivers = loadedDrivers;
        protocols = loadedProtocols;
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
        
        // Add Conversions
        module.AddConversions(GetConversions(xmlModule.Conversions));

        // Add Presentations
        module.AddPresentations(GetPresentations(module.Conversions, xmlModule.Presentations));

        // Add variables
        module.AddVariables(GetVariables(module.Presentations, xmlModule.Variables));
        
        
        // Add project drivers structure - including protocols and variables, presentations, conversions
        module.AddDrivers(GetDrivers(drivers, protocols, module.Variables, xmlModule));
        
        return module;
    }

    private List<IVariableBase> GetVariables(IEnumerable<IPresentation> presentations, IEnumerable<XmlVariable> xmlVariables)
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
                variable.CommParams = xmlVariable.CommParams;

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
            logger?.Log(LogLevel.Error, $"Presentation {xmlValues.PresentationReference.Ref} not found.");
        }
        
        return values;
    }

    private List<IDriverBase> GetDrivers(IList<IDriverBase> realDrivers, IList<IProtocolBase> realProtocols, IList<IVariableBase> variables, XmlModule xmlModule)
    {
        var tempDrivers = new List<IDriverBase>();
        
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
            var driver = realDrivers.FirstOrDefault(rd => rd.Specification.Name == xmlDriver.Name);
            if (driver == null)
            {
                logger?.Log(LogLevel.Error, $"The driver \"{xmlDriver.Name}\" not found among loaded drivers.");
                continue;
            }           
            
            driver.AddProtocols(GetProtocols(realProtocols, variables, driverRef.ProtocolReferences, xmlModule));
            
            
            tempDrivers.Add(driver);
        }
        
        return tempDrivers;
    }
    
    private IList<IProtocolBase> GetProtocols(IList<IProtocolBase> realProtocols, IList<IVariableBase> variables, IList<XmlProtocolReference> xmlProtocolReferences, XmlModule xmlModule)
    {
        var tempProtocols = new List<IProtocolBase>();
        foreach (var protocolRef in xmlProtocolReferences)
        {
            // find xml protocol based on reference in protocol references
            var xmlProtocol = xmlModule.Protocols.FirstOrDefault(p => p.Name == protocolRef.Ref);
            if (xmlProtocol == null)
            {
                logger?.Log(LogLevel.Error, $"The protocol \"{protocolRef.Ref}\" not found in the module file.");
                continue;
            }
            
            var protocol = realProtocols.FirstOrDefault(p => p.Specification.Name == xmlProtocol.Name);
            if (protocol == null)
            {
                logger?.Log(LogLevel.Error, $"The protocol \"{xmlProtocol.Name}\" not found among loaded protocols.");
                continue;
            }

            protocol.AddVariables(GetProtocolVariables(variables, protocolRef.VariableReferences));
            
            tempProtocols.Add(protocol);
        }
        
        return tempProtocols;
    }
    
    private IList<IVariableBase> GetProtocolVariables(IList<IVariableBase> variables, List<XmlVariableReferenceBase> variableReferences)
    {
        var tempVariables = new List<IVariableBase>();
        
        foreach (var variableRef in variableReferences)
        {
            var variable = variables.FirstOrDefault(v => v.Id == variableRef.Ref);
            if (variable == null)
            {
                logger?.Log(LogLevel.Error, $"Variable {variableRef.Ref} not found.");
                continue;
            }
            
            tempVariables.Add(variable);
        }
        
        return tempVariables;
    }

}