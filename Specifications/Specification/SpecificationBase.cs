using System.Text.Json.Serialization;

namespace Qenex.QSuite.Specification;

public class SpecificationBase : ISpecification
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public Version Version { get; set; } = null!;
    public string? Author { get; set; }
    public string? Company { get; set; }
}