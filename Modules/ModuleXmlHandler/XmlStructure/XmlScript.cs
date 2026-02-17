using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

public class XmlScript
{
    [XmlAttribute("fileName")] public string FileName { get; set; } = string.Empty;
        [XmlAttribute("content")] public string Content { get; set; } = string.Empty;
}