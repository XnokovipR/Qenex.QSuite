using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

/// <summary>
/// Root of a standalone variables XML file (project configuration export/import).
/// Uses the same variable elements as the module XML, so variables can be moved
/// between the two by hand if needed.
/// </summary>
[Serializable]
[XmlRoot("qinsightVariables")]
public class XmlVariablesFile
{
    [XmlArray("variables")]
    [XmlArrayItem(typeof(XmlScalarVariable), ElementName = "scalarVariable")]
    [XmlArrayItem(typeof(XmlStringVariable), ElementName = "stringVariable")]
    public List<XmlVariable> Variables { get; set; } = [];
}
