using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

// Roots of standalone component XML files (project configuration export/import).
// They use the same elements as the module XML, so content can be moved between
// the two by hand if needed.

[Serializable]
[XmlRoot("qinsightPresentations")]
public class XmlPresentationsFile
{
    [XmlArray("presentations")]
    [XmlArrayItem(typeof(XmlPresentation), ElementName = "presentation")]
    public List<XmlPresentation> Presentations { get; set; } = [];
}

[Serializable]
[XmlRoot("qinsightConversions")]
public class XmlConversionsFile
{
    [XmlArray("conversions")]
    [XmlArrayItem(typeof(XmlLinearConversion), ElementName = "linearConversion")]
    [XmlArrayItem(typeof(XmlEnumConversion), ElementName = "enumConversion")]
    public List<XmlConversion> Conversions { get; set; } = [];
}

[Serializable]
[XmlRoot("qinsightVariableEvents")]
public class XmlVarEventsFile
{
    [XmlArray("variableEvents")]
    [XmlArrayItem(typeof(OnRequestXmlVarEvent), ElementName = "onRequestVariableEvent")]
    [XmlArrayItem(typeof(OnValueChangedXmlVarEvent), ElementName = "onValueChangedVariableEvent")]
    [XmlArrayItem(typeof(PeriodicXmlVarEvent), ElementName = "periodicVariableEvent")]
    public List<XmlVarEvent> Events { get; set; } = [];
}
