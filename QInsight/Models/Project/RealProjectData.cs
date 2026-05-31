using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.ModuleXmlHandler;
using Qenex.QSuite.Scripting.ScriptingEngine;
using Qenex.QSuite.UnifModule;

namespace Qenex.QInsight.Models.Project;

public class RealProjectData
{
    private ILogger? logger { get; set; }

    #region Constructors
    
    public static RealProjectData CreateRealProjectData(
        IList<PluginDetails> drvPlugins, 
        IList<PluginDetails> protocolPlugins, 
        ProjectFilesData projectFileData,
        ScriptEngineSettings scriptEngineSettings,
        ILogger? logger = null)
    {
        var xmlModuleHandler = new XmlModuleHandler(drvPlugins, protocolPlugins, logger);
        var realModule = xmlModuleHandler.CreateModule(projectFileData.Module, new UnifiedModuleFactory(), scriptEngineSettings, logger);
        
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

    public static RealProjectData CreateEmptyProjectData(
        ScriptEngineSettings scriptEngineSettings,
        ILogger? logger = null)
    {
        var realModule = new UnifiedModuleFactory().Create(scriptEngineSettings, logger);
        realModule.Specification.Label = "New Project";
        realModule.Specification.Version = new Version(1, 0, 0);
        realModule.Specification.CreatedOn = DateTime.Now;
        realModule.Specification.Modified = realModule.Specification.CreatedOn;

        return new RealProjectData(logger)
        {
            logger = logger,
            Module = realModule
        };
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
