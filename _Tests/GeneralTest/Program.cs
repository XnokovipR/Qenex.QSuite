using Qenex.QSuite.Variables.QVariables.Values;

namespace GeneralTest;

class Program
{
    static void Main(string[] args)
    {
        var val1 = new Values<int>();
        var val2 = new Values<bool>();
        
        
        val1.SetValue(123);
        val2.SetValue(true);
        
        var v1 = val1.GetValue();
        var v2 = val2.GetValue();
    }
}