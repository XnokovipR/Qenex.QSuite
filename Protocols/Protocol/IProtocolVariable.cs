using Qenex.QSuite.QVariables;

namespace Qenex.QSuite.Protocol;

public interface IProtocolVariable
{
    bool IsCommunicated { get; set; }
    IVariableBase Variable { get; set; }
    IProtVariableSpecification ProtocolVariableSpecification { get; set; }
}