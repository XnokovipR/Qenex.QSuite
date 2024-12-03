using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("protocol")]
public class XmlProtocol
{
    [XmlAttribute("name")] public string Name { get; set; } = string.Empty;
    [XmlAttribute("label")] public string Label { get; set; } = string.Empty;
    [XmlElement("version")] public string Version { get; set; } = null!;
}