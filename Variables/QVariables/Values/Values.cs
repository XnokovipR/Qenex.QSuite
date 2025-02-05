namespace Qenex.QSuite.QVariables.Values;

public class Values<T> : ValuesBase
{
    public T Value { get; set; }

    public override string ToString()
    {
        return Value.ToString();
    }
}