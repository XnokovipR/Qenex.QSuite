using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("conversion")]
public class XmlEnumConversion : XmlConversion
{
    [XmlArray("enums")]
    [XmlArrayItem(typeof(XmlEnum), ElementName = "enum")]
    public List<XmlEnum> Enums { get; set; }
}