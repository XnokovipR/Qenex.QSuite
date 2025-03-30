using Qenex.QSuite.Variables.QVariables.Values;

namespace Qenex.QSuite.Variables.QVariables;

public class ScalarVariable : VariableBase
{
    public int Size { get; set; }
    public IValuesBase Values { get; set; }
}