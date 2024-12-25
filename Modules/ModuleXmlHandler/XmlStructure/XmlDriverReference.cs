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
    [XmlAttribute("label")] public string Label { get; set; } = string.Empty;
    [XmlElement("isEnabled")] public bool IsEnabled { get; set; }
    [XmlElement("settings")] public string Settings { get; set; } = string.Empty;
    [XmlElement("encryptedSettings")] public string EncryptedSettings { get; set; } = string.Empty;

    [XmlArray("protocolReferences")]
    [XmlArrayItem(typeof(XmlProtocolReference), ElementName = "protocolReference")]
    public List<XmlProtocolReference> ProtocolReferences { get; set; } = null!;
}