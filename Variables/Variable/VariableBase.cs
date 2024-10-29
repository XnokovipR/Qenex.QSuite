using Qenex.QSuite.ProtocolVariableSpecification;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.Variable;

/// <summary>
/// The base class for all variables.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class VariableBase<T> : IVariableBase
{
    public int Id { get; set; }
    public string Guid { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ICommSpecification Specification { get; set; }
    
    /// <summary>
    /// Generic value of the variable.
    /// </summary>
    public T Value { get; set; }
}