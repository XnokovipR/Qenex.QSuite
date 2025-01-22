using Qenex.QSuite.LogSystem;

namespace Qenex.QSuite.Protocol;

public abstract class ProtVariableSpecification : IProtVariableSpecification
{
    public string Name { get; set; } = string.Empty;
}