using Qenex.QLibs.QUI;

namespace Qenex.QInsight.ViewModels;

public class SolutionExplorerViewModel(EventAggregator ea) : ViewModelBase(ea)
{
    
    #region ViewModelBase implementation

    public override string Header { get; set; } = "Solution Explorer";
    public override string Name { get; set; } = "SolutionExplorerViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Left;
    public override bool IsDocument => false;
    
    public override void Exit()
    {
    }

    #endregion

}