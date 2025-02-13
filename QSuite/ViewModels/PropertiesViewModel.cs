using Qenex.QLibs.QUI;
using Qenex.QSuite.LogSystem;
using Syncfusion.Windows.Tools.Controls;

namespace Qenex.QSuite.ViewModels;

public class PropertiesViewModel : ViewModelBase
{
    #region Constructors

    public PropertiesViewModel(EventAggregator ea) : base(ea)
    {
    }

    #endregion
    
    #region Properties

    #region BaseViewModel implementation

    public override string Header
    {
        get => "Properties";
        set { }
    }

    public override string Name
    {
        get => "PropertiesViewModel";
        set { }
    }

    public override DockSide DockingPosition
    {
        get => DockSide.Right;
        set { }
    }

    public override bool IsDocument => false;
    public override bool CanClose => false;

    #endregion

    #endregion    
}