using Qenex.QSuite.CoreComm;
using Qenex.QSuite.CoreModule;
using Qenex.QSuite.Driver;
using Qenex.QSuite.Specification;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.Module;

public interface IModuleBase : ICoreModuleBase, ICoreCommunication
{
    IList<IVariableBase> Variables { get; }
    IDriverBase? Driver { get; set; }
}