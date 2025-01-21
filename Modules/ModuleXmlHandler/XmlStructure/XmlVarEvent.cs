using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

public abstract class XmlVarEvent
{
    [XmlAttribute("name")] public string Name { get; set; } = string.Empty;
}
