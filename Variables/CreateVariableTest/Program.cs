using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Variables.Tests.CreateVariableTest;

class Program
{
    static void Main(string[] args)
    {
        // IValuesBase var1 = ValuesGlobal.CreateInstance(ValuesGlobal.ValueDataType.Double);
        // IValuesBase var2 = ValuesGlobal.CreateInstance(ValuesGlobal.ValueDataType.Int32);
        // IValuesBase var3 = ValuesGlobal.CreateInstance(ValuesGlobal.ValueDataType.String);
        
        
        IVariableBase var1 = new ScalarVariable();
        var a = typeof(ScalarVariable);
        
        IVariableBase var2 = VariablesGlobal.CreateInstance(VariablesGlobal.VariableType.Scalar);
    }
}