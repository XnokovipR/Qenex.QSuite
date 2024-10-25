namespace Qenex.QSuite.Specification;

public class Specification : ISpecification
{
    public string Guid { get; }
    public string Name { get; }
    public string Description { get; }
    public string Author { get; }
    public DateTime ReleaseDate { get; }
    public Version Version { get; }
}