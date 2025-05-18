using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.ModuleXmlHandler;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.UnifModule;

namespace Qenex.QSuite.Models.Project;

public class RealProjectData
{
    private ILogger? logger { get; set; }

    #region Constructors
    
    public static RealProjectData CreateRealProjectData(IList<PluginDetails> drvPlugins, IList<PluginDetails> protocolPlugins, ProjectFilesData projectFileData, ILogger? logger = null)
    {
        var xmlModuleHandler = new XmlModuleHandler(drvPlugins, protocolPlugins, logger);
        var realModule = xmlModuleHandler.CreateModule<UnifiedModule>(projectFileData.Module);
        
        if (realModule == null)
        {
            logger?.Log(LogLevel.Error, "Failed to create real module from XML module.");
            return null!;
        }

        var realProjectData = new RealProjectData(logger)
        {
            logger = logger,
            Module = realModule
        };
        
        return realProjectData;
    }

    public RealProjectData(ILogger? logger = null)
    {
        this.logger = logger;
    }

    #endregion


    #region Project file properties

    public UnifiedModule Module { get; set; } = null!;

    #endregion
    
}