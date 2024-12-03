using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("values")]
public class XmlValues
{
    [XmlAttribute("dataType")] public QDataType DataType { get; set; }
    [XmlAttribute("size")] public int Size { get; set; }
    [XmlAttribute("length")] public int Length { get; set; }
    
    [XmlElement("presentationReference")] public XmlPresentationReference PresentationReference { get; set; } = null!;
}