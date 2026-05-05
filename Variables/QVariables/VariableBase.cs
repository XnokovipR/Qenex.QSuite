using System.Xml.Serialization;
using Qenex.QSuite.Specifications.ComponentSpecification;
using Qenex.QSuite.Specifications.ProtocolVariableSpecification;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Variables.QVariables;

/// <summary>
/// The base class for all variables.
/// </summary>
public abstract class VariableBase : IVariableBase
{
    #region Properties

    public int Id { get; set; }
    
    public string Namespace { get; set; }
    public string Name { get; set; }
    
    public string Label { get; set; }
    
    public string Description { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Reference to the modules to which the variable data will be sent.
    /// </summary>
    public IList<IComponentSpecification> CommComponents { get; set; }

    #endregion

    #region Methods

    public abstract object GetValue();
    public abstract void SetValue(object value);

    #endregion
}