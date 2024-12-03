using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

public enum XmlValuesDataType
{
    [XmlEnum("byte")] Byte,
    [XmlEnum("int")] Int,
    [XmlEnum("float")] Float,
    [XmlEnum("string")] String,
}