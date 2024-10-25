using Qenex.QSuite.Variable;

namespace Qenex.QSuite.Variable;

public abstract class VariableBase<T> : IVariable
{
    public int Id { get; set; }
    public string Guid { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    
    public T Value { get; set; }
}