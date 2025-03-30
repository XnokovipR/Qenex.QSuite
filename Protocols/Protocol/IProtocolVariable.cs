using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Protocols.Protocol;

public interface IProtocolVariable
{
    bool IsCommunicated { get; set; }
    IVariableBase Variable { get; set; }
    IProtVariableSpecification ProtocolVariableSpecification { get; set; }
}