using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

public enum QDataType
{
    [XmlEnum("bool")] Bool,
    [XmlEnum("byte")] Byte,
    [XmlEnum("int32")] Int32,
    [XmlEnum("float")] Float,
    [XmlEnum("string")] String,
}