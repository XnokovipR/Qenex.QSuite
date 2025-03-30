using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QSuite.Protocols.Protocol;

public abstract class ProtVariableSpecification : IProtVariableSpecification
{
    public string Name { get; set; } = string.Empty;
}