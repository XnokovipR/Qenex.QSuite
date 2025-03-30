using Qenex.QLibs.QUI;
using Qenex.QSuite.LogSystems.LogSystem;
using Syncfusion.Windows.Tools.Controls;

namespace Qenex.QSuite.ViewModels;

public class SolutionExplorerViewModel : ViewModelBase
{
    #region Constructors
    
    public SolutionExplorerViewModel(EventAggregator ea) : base(ea)
    {
    }
    
    #endregion
    
    #region Properties

    #region BaseViewModel implementation

    public override string Header
    {
        get => "Solution Explorer";
        set { }
    }

    public override string Name
    {
        get => "SolutionExplorerViewModel";
        set { }
    }

    public override DockSide DockingPosition
    {
        get => DockSide.Left;
        set { }
    }

    public override bool IsDocument => false;
    public override bool CanClose => false;

    #endregion

    #endregion    
}