using System.Text.Json.Serialization;

namespace Qenex.QSuite.Specification;

public class SpecificationBase : ISpecification
{
    [JsonPropertyName("gid")]
    public Guid Gid { get; init; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("createdOn")]
    public DateTime CreatedOn { get; set; }
    
    [JsonPropertyName("version")]
    public Version Version { get; set; } = null!;
    
    [JsonPropertyName("author")]
    public string? Author { get; set; }
    
    [JsonPropertyName("company")]
    public string? Company { get; set; }
}