namespace Qenex.QSuite.Variable;

public interface IVariable
{
    int Id { get; set; }
    string Guid { get; set; }
    string Name { get; set; }
    string Description { get; set; }    
}