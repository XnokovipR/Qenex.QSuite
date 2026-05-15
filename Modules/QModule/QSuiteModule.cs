using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Scripting.ScriptingEngine;
using Qenex.QSuite.UnifModule;

namespace Qenex.QSuite.Modules.QModule;

/// <summary>
/// QSuite module servers as a model fot QSuite graphic environment
/// </summary>
public sealed class QSuiteModule(ScriptEngineSettings scriptEngineSettings, ILogger? logger) : UnifiedModule(scriptEngineSettings, logger)
{
    
}
