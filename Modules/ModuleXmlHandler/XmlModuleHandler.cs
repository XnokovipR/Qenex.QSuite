using Qenex.QSuite.Driver;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Module;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.Protocol;
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
        foreach (var xmlVariable in xmlModule.Variables)
        {

        }



        return module;
    }

}