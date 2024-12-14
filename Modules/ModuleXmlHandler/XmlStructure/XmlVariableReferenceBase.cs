using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;


public abstract class XmlVariableReferenceBase
{
    [XmlAttribute("ref")] public int Ref { get; set; }
    [XmlAttribute("isCommunicated")] public bool IsCommunicated { get; set; }
    [XmlElement("commParam")] public string CommParam { get; set; } = string.Empty;
}