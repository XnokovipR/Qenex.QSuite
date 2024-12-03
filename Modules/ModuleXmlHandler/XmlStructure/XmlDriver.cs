using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("driver")]
public class XmlDriver
{
    [XmlAttribute("name")] public string Name { get; set; } = string.Empty;
    [XmlAttribute("label")] public string Label { get; set; } = string.Empty;
    [XmlElement("version")] public string Version { get; set; } = null!;
    [XmlElement("isEnabled")] public bool IsEnabled { get; set; }
    [XmlElement("settings")] public string Settings { get; set; } = string.Empty;
}