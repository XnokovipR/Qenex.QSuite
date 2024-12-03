using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("linearConversion")]
public class XmlLinearConversion : XmlConversion
{
    [XmlAttribute("multiplier")] public double Multiplier { get; set; }
    [XmlAttribute("offset")] public double Offset { get; set; }
}