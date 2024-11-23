namespace Qenex.QSuite.Variable;

public class Int32Variable : RangeVariable<int>
{
    public Int32Variable()
    {
        Length = sizeof(int);
        Value = 0;
    }
}