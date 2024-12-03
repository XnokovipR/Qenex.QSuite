using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("scalarVariable")]
public class XmlScalarVariable : XmlVariable
{
    [XmlAttribute("size")] public int Size { get; set; }
    [XmlElement("values")] public XmlValues Values { get; set; } = null!;
}