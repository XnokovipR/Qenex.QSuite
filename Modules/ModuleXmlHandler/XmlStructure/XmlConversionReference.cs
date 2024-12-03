using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("conversionReference")]
public class XmlConversionReference
{
    [XmlAttribute("ref")] public string Ref { get; set; } = string.Empty;
}