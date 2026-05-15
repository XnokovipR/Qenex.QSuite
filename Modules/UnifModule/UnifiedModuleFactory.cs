using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Modules.Module;
using Qenex.QSuite.Scripting.ScriptingEngine;

namespace Qenex.QSuite.UnifModule;

public class UnifiedModuleFactory : IModuleFactory<UnifiedModule>
{
    public UnifiedModule Create(ScriptEngineSettings scriptEngineSettings, ILogger? logger) => new UnifiedModule(scriptEngineSettings, logger);
}
