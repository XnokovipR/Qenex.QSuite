using Qenex.QSuite.Variables.ValuePresentation;

namespace Qenex.QSuite.Variables.QVariables.Values;

public abstract class ValuesBase : IValuesBase
{
    public ValuesGlobal.ValueDataType ValueType { get; set; }
    public int Size { get; set; }
    public int Length { get; set; }

    public IPresentation ValPresentation { get; set; }
    
    public abstract object GetValue();
    public abstract void SetValue(object value);
}