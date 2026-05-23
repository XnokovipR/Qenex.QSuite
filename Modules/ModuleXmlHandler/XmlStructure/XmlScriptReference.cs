using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("script")]
public class XmlScriptReference
{
    [XmlAttribute("ref")] public string Ref { get; set; } = string.Empty;
    [XmlAttribute("additionalInfo")] public string AdditionalInfo { get; set; } = string.Empty;
}
