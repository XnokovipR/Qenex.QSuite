using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

public class XmlScript
{
    [XmlAttribute("fileName")] public string FileName { get; set; } = string.Empty;
    [XmlAttribute("content")] public string Content { get; set; } = string.Empty;
	[XmlAttribute("executionMode")] public XmlScriptExecutionMode ExecutionMode { get; set; } = XmlScriptExecutionMode.Manual;
	[XmlAttribute("additionalInfo")] public string AdditionalInfo { get; set; } = string.Empty;

}

public enum XmlScriptExecutionMode
{
	Manual,
	Periodic,
	OnValueChanged,
	Startup
}