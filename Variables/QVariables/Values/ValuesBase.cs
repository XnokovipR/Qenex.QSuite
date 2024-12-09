using Qenex.QSuite.ValuePresentation;

namespace Qenex.QSuite.QVariables.Values;

public class ValuesBase : IValuesBase
{
    public ValuesGlobal.ValueDataType ValueType { get; set; }
    public int Size { get; set; }
    public int Length { get; set; }

    public IPresentation ValPresentation { get; set; }
}