using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("variableEmbeddedReference")]
public class XmlVariableEmbeddedReference : XmlVariableReferenceBase
{
    [XmlElement("address")] public string Address { get; set; } = string.Empty;
    [XmlElement("eventReference")] public XmlEventReference EventReference { get; set; }
}