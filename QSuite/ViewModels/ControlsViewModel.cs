using Qenex.QLibs.QUI;
using Syncfusion.Windows.Tools.Controls;

namespace Qenex.QSuite.ViewModels;

public class ControlsViewModel : ViewModelBase
{
    #region Constructors

    public ControlsViewModel(EventAggregator ea) : base(ea)
    {
    }

    #endregion
    
    #region Properties

    #region BaseViewModel implementation

    public override string Header
    {
        get => "Controls";
        set { }
    }

    public override string Name
    {
        get => "ControlssViewModel";
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