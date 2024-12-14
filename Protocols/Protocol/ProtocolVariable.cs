using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.QVariables;

namespace Qenex.QSuite.Protocol;

public class ProtocolVariable : IProtocolVariable
{
    public IVariableBase Variable { get; set; }
}