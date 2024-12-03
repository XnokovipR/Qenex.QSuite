using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;


public abstract class XmlVariableReferenceBase
{
    [XmlAttribute("ref")] public string Ref { get; set; } = string.Empty;
    [XmlAttribute("isCommunicated")] public bool IsCommunicated { get; set; }
}