using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("periodicVariableEvent")]
public class PeriodicXmlVarEvent : XmlVarEvent
{
	[XmlAttribute("period")] public int Period { get; set; }
	[XmlAttribute("unit")] public string Unit { get; set; } = string.Empty;
}
