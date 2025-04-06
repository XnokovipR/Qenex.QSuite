using Qenex.QLibs.QUI;
using Qenex.QSuite.LogSystems.LogSystem;
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

    public override string Identifier => $"PropertiesViewModel:{Guid.NewGuid()}";

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

    public override DockState DockState { get; set; }

    public override bool CanMaximize => false;
    public override bool IsDocument => false;
    public override bool CanClose => false;
    public override bool CanSerialize => true;

    #endregion

    #endregion    
}