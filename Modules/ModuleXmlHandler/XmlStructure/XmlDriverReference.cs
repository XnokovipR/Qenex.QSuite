using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

/// <summary>
/// 
/// </summary>
[Serializable]
[XmlRoot("driverReference")]
public class XmlDriverReference
{
    [XmlAttribute("ref")] public string Ref { get; set; } = string.Empty;
    
    [XmlArray("protocolReferences")]
    [XmlArrayItem(typeof(XmlProtocolReference), ElementName = "protocolReference")]
    public List<XmlProtocolReference> ProtocolReferences { get; set; }
}