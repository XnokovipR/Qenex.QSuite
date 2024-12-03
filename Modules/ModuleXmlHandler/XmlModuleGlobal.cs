using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.QVariables;

namespace Qenex.QSuite.ModuleXmlHandler;

public class XmlModuleGlobal
{
    public static readonly Dictionary<Type, VariablesGlobal.VariableType> TypeOfXmlVariable2VariableEnumDict = new Dictionary<Type, VariablesGlobal.VariableType>()
    {
        { typeof(XmlScalarVariable), VariablesGlobal.VariableType.Scalar },
        { typeof(XmlStringVariable), VariablesGlobal.VariableType.String }
    };  
    
    
    

}