using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.QVariables;

namespace Qenex.QSuite.Protocol;

public class ProtocolVariable : IProtocolVariable
{
    public bool IsCommunicated { get; set; }
    public IVariableBase Variable { get; set; }
    public IProtVariableSpecification ProtocolVariableSpecification { get; set; }
    
}