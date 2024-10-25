namespace Qenex.QSuite.Variable;

public class Int32VariableBase : VariableBase<int>
{
    public ValueRange<int> RawRange { get; set; }
    public ValueRange<int> EngRange { get; set; }
    public string Unit { get; set; }
}