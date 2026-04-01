using Qenex.QLibs.QUI;

namespace Qenex.QInsight.ViewModels;

public class ScriptVariablesSettingsViewModel : WorkspaceViewModelBase
{
    #region Constructors

    public ScriptVariablesSettingsViewModel(EventAggregator ea) : base(ea)
    {
    }

    #endregion
    #region ViewModelBase implementation

    public override string Header { get; set; } = "";
    public override string Name { get; set; } = "ScriptVariablesSettingsViewModel";
    public override string WinTitle { get => "Script-Variables Settings"; set { return; } }
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Workspace;
    public override bool IsDocument => true;


    #endregion
}