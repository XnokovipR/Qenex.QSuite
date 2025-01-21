using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;
[Serializable]
[XmlRoot("onValueChangedVariableEvent")]
public class OnValueChangedXmlVarEvent : XmlVarEvent
{
	[XmlAttribute("threshold")] public double Threshold { get; set; }
}
