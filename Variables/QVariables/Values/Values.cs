namespace Qenex.QSuite.Variables.QVariables.Values;

public class Values<T> : ValuesBase
{
    public T Value { get; set; }

    public override object GetValue()
    {
        return Value;
    }

    public override void SetValue(object value)
    {
        if (value is T typedValue)
        {
            Value = typedValue;
        }
        else
        {
            throw new InvalidCastException($"Cannot cast value of type {value.GetType()} to {typeof(T)}.");
        }
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}