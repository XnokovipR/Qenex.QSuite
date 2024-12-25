using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("driver")]
public class XmlDriver
{
    [XmlAttribute("id")] public int Id { get; set; }
    [XmlAttribute("name")] public string Name { get; set; } = string.Empty;
    [XmlElement("version")] public string Version { get; set; } = null!;
}