using System.Xml.Serialization;

namespace Qenex.QSuite.ModuleXmlHandler.XmlStructure;

[Serializable]
[XmlRoot("variableReference")]
public class XmlVariableReference : XmlVariableReferenceBase
{
    // Ref and IsCommunicated are defied in the base class
}