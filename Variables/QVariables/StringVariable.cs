namespace Qenex.QSuite.Variables.QVariables;

public class StringVariable : VariableBase
{
    public int Size { get; private set; }

    private string values;
    public string Values
    {
        get => values;
        set { values = value; Size = value.Length; }
    }

    public override object GetValue()
    {
        return Values;
    }

    public override void SetValue(object value)
    {
        Values = value.ToString();
    }
}