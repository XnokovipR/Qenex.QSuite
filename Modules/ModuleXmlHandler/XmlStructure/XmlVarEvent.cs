using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

public abstract class XmlVarEvent
{
    [XmlAttribute("name")] public string Name { get; set; } = string.Empty;

    [XmlAttribute("eventExtraParams")] public string EventExtraParams { get; set; } = string.Empty;

    // Keeps the attribute out of the XML for ordinary events (XmlSerializer convention).
    public bool ShouldSerializeEventExtraParams() => !string.IsNullOrEmpty(EventExtraParams);
}
