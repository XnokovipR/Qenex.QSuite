namespace Qenex.QSuite.Specification;

public interface ISpecification
{
    string Guid { get; }
    string Name { get; }
    string Description { get; }
    string Author { get; }
    DateTime ReleaseDate { get; }
    Version Version { get; }
}