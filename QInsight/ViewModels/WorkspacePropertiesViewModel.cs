using Qenex.QLibs.QUI;

namespace Qenex.QInsight.ViewModels;

public class WorkspacePropertiesViewModel(EventAggregator ea, string workspaceHeader) : ViewModelBase(ea)
{

    #region ViewModelBase implementation

    public override string Header { get; set; } = "Work Properties";
    public override string Name { get; set; } = "WorkPropertiesViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Right;
    public override bool IsDocument => false;

    #endregion
}