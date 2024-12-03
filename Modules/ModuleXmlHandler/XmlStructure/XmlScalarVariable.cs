using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("scalarVariable")]
public class XmlScalarVariable : XmlVariable
{
    [XmlElement("values")] public XmlValues Values { get; set; } = null!;
}