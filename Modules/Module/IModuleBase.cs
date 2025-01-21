using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.Driver;
using Qenex.QSuite.Specification;
using Qenex.QSuite.QVariables;
using Qenex.QSuite.ValuePresentation;
using Qenex.QSuite.ValueConversion;
using Qenex.QSuite.VariableEvents;

namespace Qenex.QSuite.Module;

public interface IModuleBase : IComponentSpecification, ICoreCommunication
{
    IList<IVariableBase> Variables { get; set; }
    IList<IDriverBase> Drivers { get; set; }
    IList<IPresentation> Presentations { get; set; }
    IList<IValConversion> Conversions { get; set; }
    IList<IVarEvent> VarEvents { get; set; }
    
    
    void AddVariable(IVariableBase variable);
    void AddVariables(IList<IVariableBase> variables);
    void RemoveVariable(IVariableBase variable);
    
    void AddDriver(IDriverBase driver);
    void AddDrivers(IEnumerable<IDriverBase> drivers);
    void RemoveDriver(IDriverBase driver);

    void AddPresentation(IPresentation presentation);
    void AddPresentations(IList<IPresentation> presentations);
    void RemovePresentation(IPresentation presentation);
    
    void AddConversion(IValConversion conversion);
    void AddConversions(IList<IValConversion> conversions);
    void RemoveConversion(IValConversion conversion);
    
    void AddVarEvent(IVarEvent varEvent);
    void AddVarEvents(IList<IVarEvent> varEvents);
    void RemoveVarEvent(IVarEvent varEvent);
}