using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
public class XmlMatrixSection
{
    /// <summary>Number of values (not bytes). For the data section of a curve/map it is derived and informative.</summary>
    [XmlAttribute("count")] public int Count { get; set; }

    /// <summary>Axis caption; omitted when empty (null).</summary>
    [XmlAttribute("label")] public string? Label { get; set; }

    /// <summary>Element type override; omitted when the section uses the variable default.</summary>
    [XmlAttribute("dataType")] public XmlValuesDataType DataType { get; set; }
    [XmlIgnore] public bool DataTypeSpecified { get; set; }

    [XmlElement("presentationReference")] public XmlPresentationReference? PresentationReference { get; set; }
}
