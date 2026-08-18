using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("scalarVariable")]
public class XmlScalarVariable : XmlVariable
{
    [XmlAttribute("size")] public int Size { get; set; }

    // Optional bit-field extraction in text form (">>2" / "<<3", "0x0F"); absent = none (older projects).
    [XmlAttribute("bitShift")] public string? BitShift { get; set; }
    [XmlAttribute("bitMask")] public string? BitMask { get; set; }
    [XmlElement("values")] public XmlValues Values { get; set; } = null!;
}