using Qenex.QSuite.CoreComm;
using Qenex.QSuite.Specification;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.Protocol;

/// <summary>
/// Common interface for all protocols.
/// </summary>
public interface IProtocolBase
{
    /// <summary>
    /// Protocol specification.
    /// </summary>
    ISpecification Specification { get;  }
    
    /// <summary>
    /// Variables that are communicated with the protocol.
    /// </summary>
    IList<IProtocolVariable> ProtocolVariables { get; set; }
}