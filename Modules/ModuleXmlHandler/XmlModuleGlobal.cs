using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.QVariables;
using Qenex.QSuite.ValueConversion;

namespace Qenex.QSuite.ModuleXmlHandler;

public class XmlModuleGlobal
{
    public static readonly Dictionary<Type, VariablesGlobal.VariableType> TypeOfXmlVariable2VariableEnumDict = new Dictionary<Type, VariablesGlobal.VariableType>()
    {
        { typeof(XmlScalarVariable), VariablesGlobal.VariableType.Scalar },
        { typeof(XmlStringVariable), VariablesGlobal.VariableType.String }
    };  
    
    
    public static readonly Dictionary<Type, ConversionsGlobal.ConversionType> TypeOfXmlConversion2ConversionEnumDict = new Dictionary<Type, ConversionsGlobal.ConversionType>()
    {
        { typeof(XmlLinearConversion), ConversionsGlobal.ConversionType.Linear },
        { typeof(XmlEnumConversion), ConversionsGlobal.ConversionType.Enum }
    };  

}