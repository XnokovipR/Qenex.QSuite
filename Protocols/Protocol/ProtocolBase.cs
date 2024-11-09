using Qenex.QSuite.CoreComm;
using Qenex.QSuite.Specification;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.Protocol;

/// <summary>
/// Base class for all protocols.
/// </summary>
public abstract class ProtocolBase : IProtocolBase
{
    public ISpecification Specification { get; } = new SpecificationBase();
    
    public IList<IProtocolVariable> ProtocolVariables { get; set; }
}