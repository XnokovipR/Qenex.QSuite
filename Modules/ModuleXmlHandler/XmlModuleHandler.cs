using Qenex.QSuite.Driver;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Module;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.QVariables;
using Qenex.QSuite.QVariables.Values;
using Qenex.QSuite.UnifModule;

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
        
        if (module.Specification.Name!= xmlModule.Name)
        {
            logger?.Log(LogLevel.Error, "Module Name does not match with the UnifiedModule Name");
            return null;
        }

        // Add module specification
        module.Specification.Label = xmlModule.Label;
        module.Specification.Description = xmlModule.Description;
        module.Specification.Version = new Version(xmlModule.Version);
        module.Specification.Author = xmlModule.Author;
        module.Specification.Company = xmlModule.Company;
        module.Specification.CreatedOn = xmlModule.CreatedOn;
        
        // Add variables
        module.AddVariableRange(CreateVariables(xmlModule.Variables));



        return module;
    }

    private List<IVariableBase> CreateVariables(IEnumerable<XmlVariable> xmlVariables)
    {
        var variables = new List<IVariableBase>();
        
        foreach (var xmlVariable in xmlVariables)
        {
            if (!XmlModuleGlobal.TypeOfXmlVariable2VariableEnumDict.TryGetValue(xmlVariable.GetType(), out var variableType))
            {
                logger?.Log(LogLevel.Error, $"Variable type {xmlVariable.GetType()} is not supported");
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
                    scalarVariable.Values = CreateScalarValues(xmlScalarVariable.Values);
                }
                
                
                variables.Add(variable);
            }
            catch (ArgumentException e)
            {
                logger?.Log(LogLevel.Error, $"Error creating variable {xmlVariable.Name}: {e.Message}");
            }
            
        }

        return variables;
    }

    private IValuesBase CreateScalarValues(XmlValues xmlValues)
    {
        var values = ValuesGlobal.CreateInstance(Enum.Parse<ValuesGlobal.ValueDataType>(xmlValues.DataType.ToString()));
        
        

        return values;
    }

}