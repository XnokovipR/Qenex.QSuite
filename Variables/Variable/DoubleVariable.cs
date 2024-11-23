namespace Qenex.QSuite.Variable;

public class DoubleVariable : RangeVariable<double>
{
    public DoubleVariable()
    {
        Length = sizeof(double);
        Value = 0.0;
    }
}