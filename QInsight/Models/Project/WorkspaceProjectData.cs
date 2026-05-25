using System.Xml.Serialization;
using Qenex.QInsight.ViewModels;

namespace Qenex.QInsight.Models.Project;

[XmlRoot("workspace")]
public class WorkspaceProjectData
{
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute("winTitle")]
    public string WinTitle { get; set; } = string.Empty;

    public static WorkspaceProjectData FromWorkspace(WorkspaceViewModel workspace)
    {
        return new WorkspaceProjectData
        {
            Name = workspace.Name,
            WinTitle = workspace.WinTitle
        };
    }
}
