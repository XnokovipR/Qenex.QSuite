using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

public class XmlConversion
{
    [XmlAttribute("name")] public string Name { get; set; } = string.Empty;
}