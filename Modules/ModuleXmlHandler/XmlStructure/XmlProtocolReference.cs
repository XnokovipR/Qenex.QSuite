using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("protocolReference")]
public class XmlProtocolReference
{
    [XmlAttribute("ref")] public string Ref { get; set; } = string.Empty;
    
    [XmlElement("settings")] public string Settings { get; set; } = string.Empty;
    [XmlElement("encryptedSettings")] public string EncryptedSettings { get; set; } = string.Empty;
    
    [XmlArray("variableReferences")]
    [XmlArrayItem(typeof(XmlVariableReference), ElementName = "variableReference")]
    public List<XmlVariableReferenceBase> VariableReferences { get; set; }
}