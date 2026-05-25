using System.Runtime.Serialization;
using Qenex.QInsight.ViewModels;
using Qenex.QSuite.Controls.Control;

namespace Qenex.QInsight.Models.Project;

[DataContract]
public class WorkspaceProjectData
{
    [DataMember]
    public string Name { get; set; } = string.Empty;

    [DataMember]
    public string WinTitle { get; set; } = string.Empty;

    [DataMember]
    public List<ControlBase> Controls { get; set; } = [];

    public static WorkspaceProjectData FromWorkspace(WorkspaceViewModel workspace)
    {
        return new WorkspaceProjectData
        {
            Name = workspace.Name,
            WinTitle = workspace.WinTitle,
            Controls = workspace.GetControlProjectData()
        };
    }
}
