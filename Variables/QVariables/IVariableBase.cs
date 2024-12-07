using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.ProtocolVariableSpecification;

namespace Qenex.QSuite.QVariables;

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
    /// Namespace specifies the location of the variable.
    /// </summary>
    string Namespace { get; set; }
    
    /// <summary>
    /// Name of the variable.
    /// </summary>
    string Name { get; set; }
    
    /// <summary>
    /// Label of the variable (in most case name and label differs).
    /// </summary>
    string Label { get; set; }
    
    /// <summary>
    /// Description of the variable.
    /// </summary>
    string Description { get; set; }
    
    /// <summary>
    /// Communication parameters for the variable: details how to communicate via protocol (e.g. address in communicated device).
    /// </summary>
    string CommParams { get; set; }
    
    /// <summary>
    /// Reference to the components (modules / drivers) to which the variable data will be sent.
    /// </summary>
    IList<IComponentSpecification> CommComponents { get; set; }
}