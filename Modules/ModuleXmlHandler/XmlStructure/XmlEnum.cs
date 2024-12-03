using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("enum")]
public class XmlEnum
{
    [XmlAttribute("name")] public string Name { get; set; } = string.Empty;
    [XmlAttribute("value")] public int Value { get; set; } 
}