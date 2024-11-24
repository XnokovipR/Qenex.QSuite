using Qenex.QSuite.CoreComm;
using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.Driver;
using Qenex.QSuite.Specification;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.Module;

public interface IModuleBase : IComponentSpecification, ICoreCommunication
{
    IList<IVariableBase> Variables { get; set; }
    IList<IDriverBase> Drivers { get; set; }
    
    void AddVariable(IVariableBase variable);
    void AddVariableRange(IEnumerable<IVariableBase> variables);
    void RemoveVariable(IVariableBase variable);
    void AddDriver(IDriverBase driver);
    void RemoveDriver(IDriverBase driver);
}