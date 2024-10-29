namespace Qenex.QSuite.Specification;

/// <summary>
/// The specific interface uniquely identifies Qenex modules, drivers, protocols, and also assigning a protocol to a variable.   
/// </summary>
public interface ISpecification
{
    /// <summary>
    /// A unique identifier of the specification.
    /// </summary>
    string Guid { get; }
    
    /// <summary>
    /// A name of the specification.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// A description of the specification.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// The release date of the specification.
    /// </summary>
    DateTime ReleaseDate { get; }
    
    /// <summary>
    /// The version of the specification.
    /// </summary>
    Version Version { get; }
}