using Qenex.QSuite.CoreModule;
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
    /// Description of the variable.
    /// </summary>
    string Description { get; set; }
    
    /// <summary>
    /// The specification of the variable.
    /// </summary>
    ICommSpecification Specification { get; set; }
    
    List<ICoreModuleBase> CommModules { get; set; }
}