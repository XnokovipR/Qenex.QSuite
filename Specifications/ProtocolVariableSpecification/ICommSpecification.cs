using Qenex.QSuite.Specification;

namespace Qenex.QSuite.ProtocolVariableSpecification;

/// <summary>
/// A Communication specification uniquely identifies a variable in a given protocol.
/// It contains the protocol specification, and information how to communicate with the variable through the protocol.
/// </summary>
public interface ICommSpecification
{
    /// <summary>
    /// Link to a protocol specification.
    /// </summary>
    ISpecification ProtocolSpecification { get; set; }
}