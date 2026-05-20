using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

public enum XmlValuesDataType
{
    [XmlEnum("byte")] Byte,
    [XmlEnum("ushort")] UShort,
    [XmlEnum("uint")] UInt,
    [XmlEnum("ulong")] ULong,
    [XmlEnum("sbyte")] SByte,
    [XmlEnum("short")] Short,
    [XmlEnum("int")] Int,
    [XmlEnum("long")] Long,
    [XmlEnum("float")] Float,
    [XmlEnum("double")] Double,
    [XmlEnum("string")] String,
}
