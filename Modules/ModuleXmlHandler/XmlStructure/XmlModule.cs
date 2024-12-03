using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

/// <summary>
/// Xml Module store all the data about module structure - how the drivers, protocols, and variables are related.  
/// </summary>
[Serializable]
[XmlRoot("module")]
public class XmlModule
{
    // Module information
    [XmlAttribute("name")] public string Name { get; set; } = string.Empty;
    [XmlAttribute("label")] public string Label { get; set; } = string.Empty;
    [XmlElement("description")] public string Description { get; set; } = string.Empty;
    [XmlElement("version")] public string Version { get; set; } = null!;
    [XmlElement("author")] public string Author { get; set; } = string.Empty;
    [XmlElement("company")] public string Company { get; set; } = string.Empty;
    [XmlElement("createdOn")] public DateTime CreatedOn { get; set; }
    
    // Driver relations
    [XmlArray("driverReferences")]
    [XmlArrayItem(typeof(XmlDriverReference), ElementName = "driverReference")]
    public List<XmlDriverReference> DriverReferences { get; set; }
    
    // Drivers
    [XmlArray("drivers")]
    [XmlArrayItem(typeof(XmlDriver), ElementName = "driver")]
    public List<XmlDriver> Drivers { get; set; }
    
    // Protocols
    [XmlArray("protocols")]
    [XmlArrayItem(typeof(XmlProtocol), ElementName = "protocol")]
    public List<XmlProtocol> Protocols { get; set; }
    
    // Presentations
    [XmlArray("presentations")]
    [XmlArrayItem(typeof(XmlPresentation), ElementName = "presentation")]
    public List<XmlPresentation> Presentations { get; set; }
    
    // conversions
    [XmlArray("conversions")]
    [XmlArrayItem(typeof(XmlLinearConversion), ElementName = "linearConversion")]
    [XmlArrayItem(typeof(XmlEnumConversion), ElementName = "enumConversion")]
    public List<XmlConversion> Conversions { get; set; }
    

    // Variables
    [XmlArray("variables")]
    [XmlArrayItem(typeof(XmlScalarVariable), ElementName = "scalarVariable")]
    [XmlArrayItem(typeof(XmlStringVariable), ElementName = "stringVariable")]
    public List<XmlVariable> Variables { get; set; }
}