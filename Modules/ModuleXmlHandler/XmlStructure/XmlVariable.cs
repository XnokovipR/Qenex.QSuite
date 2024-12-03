using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;


public abstract class XmlVariable
{
    [XmlAttribute("id")] public int Id { get; set; }
    [XmlAttribute("namespace")] public string Namespace { get; set; } = string.Empty;
    [XmlAttribute("name")] public string Name { get; set; } = string.Empty;
    [XmlAttribute("label")] public string Label { get; set; } = string.Empty;
    
    [XmlElement("description")] public string Description { get; set; } = string.Empty;
}