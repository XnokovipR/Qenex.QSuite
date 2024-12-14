using Qenex.QSuite.QVariables;

namespace Qenex.QSuite.Protocol;

public interface IProtocolVariable : IProtVariableSpecification
{
    public IVariableBase Variable { get; set; }
}