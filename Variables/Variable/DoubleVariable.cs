namespace Qenex.QSuite.Variable;

public class DoubleVariableBase : VariableBase<double>
{
    public ValueRange<double> RawRange { get; set; }
    public ValueRange<double> EngRange { get; set; }
    public string Unit { get; set; }
}