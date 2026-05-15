using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Scripting.ScriptingEngine;

namespace Qenex.QSuite.Modules.Module;

public interface IModuleFactory<out T> where T : IModuleBase
{
    T Create(ScriptEngineSettings scriptEngineSettings, ILogger? logger);
}
