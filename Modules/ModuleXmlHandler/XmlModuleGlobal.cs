using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.QVariables;
using Qenex.QSuite.ValueConversion;
using Qenex.QSuite.VariableEvents;

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

    public static readonly Dictionary<VariablesGlobal.VariableType, Type> VariableEnum2TypeOfXmlVariableDict = TypeOfXmlVariable2VariableEnumDict.ToDictionary(x => x.Value, x => x.Key);

    public static readonly Dictionary<Type, EventsGlobal.VariableEventType> TypeOfXmlEvent2EventEnumDict = new Dictionary<Type, EventsGlobal.VariableEventType>()
	{
		{ typeof(PeriodicXmlVarEvent), EventsGlobal.VariableEventType.Periodic },
		{ typeof(OnRequestXmlVarEvent), EventsGlobal.VariableEventType.OnRequest },
		{ typeof(OnValueChangedXmlVarEvent), EventsGlobal.VariableEventType.OnValueChanged }
	};
}