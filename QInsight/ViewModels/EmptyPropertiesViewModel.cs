using Qenex.QLibs.QUI;

namespace Qenex.QInsight.ViewModels;

public class EmptyPropertiesViewModel(EventAggregator ea) : ViewModelBase(ea)
{

    #region ViewModelBase implementation

    public override string Header { get; set; } = "Empty Properties";
    public override string Name { get; set; } = "PropertiesViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Right;
    public override bool IsDocument => false;

    #endregion
    
}