using System.Xml.Serialization;
using Qenex.QInsight.ViewModels;

namespace Qenex.QInsight.Models.Project;

[XmlRoot("scriptDocument")]
public class ScriptDocumentProjectData
{
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute("fileName")]
    public string FileName { get; set; } = string.Empty;

    public static ScriptDocumentProjectData FromScriptViewModel(ScriptViewModel scriptViewModel)
    {
        return new ScriptDocumentProjectData
        {
            Name = scriptViewModel.Name,
            FileName = scriptViewModel.ScriptWrapper.FileName
        };
    }
}

[XmlRoot("scriptDocuments")]
public class ScriptDocumentProjectDataCollection
{
    [XmlElement("scriptDocument")]
    public List<ScriptDocumentProjectData> Documents { get; set; } = [];
}
