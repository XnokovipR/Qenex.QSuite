using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Specifications.ComponentSpecification;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.ValuePresentation;
using Qenex.QSuite.ValueConversion;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Modules.Module;

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