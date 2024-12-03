using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("presentationReference")]
public class XmlPresentationReference
{
    [XmlAttribute("ref")] public string Ref { get; set; }
}