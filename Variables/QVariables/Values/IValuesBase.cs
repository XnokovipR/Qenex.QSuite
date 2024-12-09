using Qenex.QSuite.ValuePresentation;

namespace Qenex.QSuite.QVariables.Values;

public interface IValuesBase
{
    ValuesGlobal.ValueDataType ValueType { get; set; }
    int Size { get; set; }
    int Length { get; set; }
    
    IPresentation ValPresentation { get; set; }
}