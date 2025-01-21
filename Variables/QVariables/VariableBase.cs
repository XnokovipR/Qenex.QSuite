using System.Xml.Serialization;
using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.ProtocolVariableSpecification;
using Qenex.QSuite.QVariables;
using Qenex.QSuite.VariableEvents;

namespace Qenex.QSuite.QVariables;

/// <summary>
/// The base class for all variables.
/// </summary>
public abstract class VariableBase : IVariableBase
{
    public int Id { get; set; }
    
    public string Namespace { get; set; }
    public string Name { get; set; }
    
    public string Label { get; set; }
    
    public string Description { get; set; }
    
    public IVarEvent Event { get; set; }
    
    /// <summary>
    /// Reference to the modules to which the variable data will be sent.
    /// </summary>
    public IList<IComponentSpecification> CommComponents { get; set; }
}