namespace Qenex.QSuite.Variable;

public interface IVariableBase
{
    int Id { get; set; }
    string Guid { get; set; }
    string Name { get; set; }
    string Description { get; set; }    
}