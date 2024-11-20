namespace Qenex.QSuite.Specification;

/// <summary>
/// The specific interface uniquely identifies Qenex modules, drivers, protocols, and also assigning a protocol to a variable.   
/// </summary>
public interface ISpecification
{
    /// <summary>
    /// A unique identifier of the specification.
    /// </summary>
    Guid Gid { get; init;  }
    
    /// <summary>
    /// A name of the specification.
    /// </summary>
    string Name { get; init;  }
    
    /// <summary>
    /// A description of the specification.
    /// </summary>
    string Description { get; init; }
    
    /// <summary>
    /// The release date of the specification.
    /// </summary>
    DateTime ReleaseDate { get; init; }
    
    /// <summary>
    /// The version of the specification.
    /// </summary>
    Version? Version { get; init; }
}