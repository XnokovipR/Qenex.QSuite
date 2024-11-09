namespace Qenex.QSuite.Variable;

public class RangeVariable<T> : VariableBase<T>
{
    public ValueRange<T> RawRange { get; set; }
    public ValueRange<T> EngRange { get; set; }
    public string Unit { get; set; }
}