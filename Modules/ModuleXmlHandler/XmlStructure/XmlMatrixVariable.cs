using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

public enum XmlMatrixEndianness
{
    [XmlEnum("le")] Little,
    [XmlEnum("be")] Big,
}

[Serializable]
[XmlRoot("matrixVariable")]
public class XmlMatrixVariable : XmlVariable
{
    /// <summary>Total raw buffer size in bytes; informative, recomputed from the layout on load.</summary>
    [XmlAttribute("size")] public int Size { get; set; }

    /// <summary>Default element type for sections without their own dataType.</summary>
    [XmlAttribute("dataType")] public XmlValuesDataType DataType { get; set; }

    [XmlAttribute("endianness")] public XmlMatrixEndianness Endianness { get; set; }

    [XmlElement("xAxis")] public XmlMatrixSection? XAxis { get; set; }
    [XmlElement("yAxis")] public XmlMatrixSection? YAxis { get; set; }
    [XmlElement("data")] public XmlMatrixSection Data { get; set; } = null!;
}
