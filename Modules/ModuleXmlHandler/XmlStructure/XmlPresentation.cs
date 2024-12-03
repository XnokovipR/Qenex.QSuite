using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("presentation")]
public class XmlPresentation
{
    [XmlAttribute("name")] public string Name { get; set; } = string.Empty;
    [XmlAttribute("label")] public string Label { get; set; } = string.Empty;
    [XmlAttribute("min")] public double Min { get; set; }
    [XmlAttribute("max")] public double Max { get; set; }
    [XmlAttribute("printFormat")] public string PrintFormat { get; set; } = string.Empty;
    [XmlAttribute("unit")] public string Unit { get; set; } = string.Empty;
    
    [XmlElement("conversionReference")] public XmlConversionReference ConversionReference { get; set; }
}