using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QSuite.Modules.Module;

public interface IModuleFactory<out T> where T : IModuleBase
{
    T Create(ILogger logger);
}