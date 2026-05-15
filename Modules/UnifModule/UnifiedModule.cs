using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Modules.Module;
using Qenex.QSuite.Scripting.ScriptingEngine;
using Qenex.QSuite.Specifications.Specification;

namespace Qenex.QSuite.UnifModule;

/// <summary>
/// Unified model is common for both QSuiteModel and also for Model working as a independent module executed in service.
/// </summary>
public class UnifiedModule : ModuleBase
{
    public UnifiedModule(ScriptEngineSettings scriptEngineSettings, ILogger? logger) : base(scriptEngineSettings, logger)
    {
        Specification = new SpecificationBase()
        {
            Name = "UnifiedModule",
            Label = "Unified Module"
        };
    }
}
