namespace Qenex.QSuite.Specification;

/// <summary>
/// The specific interface uniquely identifies Qenex modules, drivers, protocols, and also assigning a protocol to a variable.   
/// </summary>
public interface ISpecification
{
    /// <summary>
    /// A unique identifier of the specification.
    /// </summary>
    Guid Gid { get; set;  }
    
    /// <summary>
    /// A name of the specification.
    /// </summary>
    string Name { get; set;  }
    
    /// <summary>
    /// A description of the specification.
    /// </summary>
    string Description { get; set; }
    
    /// <summary>
    /// The release date of the specification.
    /// </summary>
    DateTime CreatedOn { get; set; }
    
    /// <summary>
    /// The version of the specification.
    /// </summary>
    Version? Version { get; set; }

    /// <summary>
    /// Name of the author
    /// </summary>
    string? Author { get; set; }
    
    /// <summary>
    /// Company name
    /// </summary>
    string? Company { get; set; }
}