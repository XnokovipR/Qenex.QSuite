using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

public class XmlScript
{
    [XmlAttribute("fileName")] public string FileName { get; set; } = string.Empty;
    [XmlAttribute("content")] public string Content { get; set; } = string.Empty;
	[XmlAttribute("executionMode")] public XmlScriptExecutionMode ExecutionMode { get; set; } = XmlScriptExecutionMode.Manual;
	[XmlAttribute("additionalInfo")] public string AdditionalInfo { get; set; } = string.Empty;
	[XmlAttribute("isEnabled")] public bool IsEnabled { get; set; }
	[XmlAttribute("isReplayEnabled")] public bool IsReplayEnabled { get; set; }

}

public enum XmlScriptExecutionMode
{
	Manual,
	Periodic,
	EventTriggered,
	OnValueChanged,
	Startup,
	Shutdown
}
