using Qenex.QSuite.Specifications.ComponentSpecification;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Protocols.Protocol;

public class ProtocolVariable : IProtocolVariable
{
    public bool IsCommunicated { get; set; }
    public IVariableBase Variable { get; set; }
    public IProtVariableSpecification ProtocolVariableSpecification { get; set; }
    
}