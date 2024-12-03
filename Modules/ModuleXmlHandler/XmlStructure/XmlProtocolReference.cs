using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("protocolReference")]
public class XmlProtocolReference
{
    [XmlAttribute("ref")] public string Ref { get; set; } = string.Empty;
    
    [XmlArray("variableReferences")]
    [XmlArrayItem(typeof(XmlVariableReference), ElementName = "variableReference")]
    [XmlArrayItem(typeof(XmlVariableEmbeddedReference), ElementName = "variableEmbeddedReference")]
    public List<XmlVariableReferenceBase> VariableReferences { get; set; }
}