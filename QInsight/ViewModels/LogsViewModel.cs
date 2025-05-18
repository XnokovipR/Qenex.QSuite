using Qenex.QLibs.QUI;

namespace Qenex.QInsight.ViewModels;

public class LogsViewModel(EventAggregator ea) : ViewModelBase(ea)
{
    
    #region ViewModelBase implementation

    public override string Header { get; set; } = "Logs";
    public override string Name { get; set; } = "LogsViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Bottom;
    public override bool IsDocument => false;
    
    public override void Exit()
    {
    }

    #endregion
}