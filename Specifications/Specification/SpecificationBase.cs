namespace Qenex.QSuite.Specification;

public class SpecificationBase : ISpecification
{
    public Guid Gid { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public DateTime ReleaseDate { get; init; }
    public Version Version { get; init; }
}