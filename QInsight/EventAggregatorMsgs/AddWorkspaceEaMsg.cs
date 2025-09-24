using Qenex.QLibs.QUI.TelerikDocking;

namespace Qenex.QInsight.EventAggregatorMsgs;

public class AddWorkspaceEaMsg
{
    public IWorkspaceViewModel WorkspaceViewModel { get; set; } = null!;
}