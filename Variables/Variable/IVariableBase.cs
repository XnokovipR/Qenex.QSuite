using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.ProtocolVariableSpecification;

namespace Qenex.QSuite.Variable;

/// <summary>
/// The interface for all variables used in the QSuite.
/// </summary>
public interface IVariableBase
{
    /// <summary>
    /// Id of the variable.
    /// </summary>
    int Id { get; set; }
    
    /// <summary>
    /// Guid of the variable.
    /// </summary>
    string Guid { get; set; }
    
    /// <summary>
    /// Name of the variable.
    /// </summary>
    string Name { get; set; }
    
    /// <summary>
    /// Caption of the variable (in most case name and caption differs].
    /// </summary>
    string Caption { get; set; }
    
    /// <summary>
    /// Description of the variable.
    /// </summary>
    string Description { get; set; }
    
    IList<IComponentSpecification> CommModules { get; set; }
}