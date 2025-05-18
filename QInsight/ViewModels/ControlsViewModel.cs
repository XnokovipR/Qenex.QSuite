using Qenex.QLibs.QUI;

namespace Qenex.QInsight.ViewModels;

public class ControlsViewModel(EventAggregator ea) : ViewModelBase(ea)
{
    #region ViewModelBase implementation

    public override string Header { get; set; } = "Controls";
    public override string Name { get; set; } = "ControlsViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Left;
    public override bool IsDocument => false;
    
    public override void Exit()
    {
    }

    #endregion
}