using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Modules.Module;

namespace Qenex.QSuite.UnifModule;

public class UnifiedModuleFactory : IModuleFactory<UnifiedModule>
{
    public UnifiedModule Create(ILogger logger) => new UnifiedModule(logger);
}