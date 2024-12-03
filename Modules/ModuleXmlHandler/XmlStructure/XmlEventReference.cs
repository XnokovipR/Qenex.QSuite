using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("eventReference")]
public class XmlEventReference
{
    [XmlAttribute("ref")] public string Ref { get; set; } = string.Empty;

}