using Qenex.QInsight.ViewModels;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Scripts.Script;

namespace Qenex.QInsight.ViewModels;

public class ScriptPropertiesViewModel(EventAggregator ea, IScriptBase s) : ViewModelBase(ea)
{
    private IScriptBase script = s;

    #region ViewModelBase implementation

    public override string Header { get; set; } = "Script Properties";
    public override string Name { get; set; } = "ScriptPropertiesViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Right;
    public override bool IsDocument => false;

    #endregion
}