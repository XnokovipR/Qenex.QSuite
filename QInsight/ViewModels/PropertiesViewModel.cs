using Qenex.QLibs.QUI;

namespace Qenex.QInsight.ViewModels;

public class PropertiesViewModel(EventAggregator ea) : ViewModelBase(ea)
{
    public IPropertiesViewModel SelectedViewModel
    {
        get;
        set
        { field = value; OnPropertyChanged(); }
    }

    #region ViewModelBase implementation

    public override string Header { get; set; } = "Properties";
    public override string Name { get; set; } = "PropertiesViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Right;
    public override bool IsDocument => false;

    #endregion
    
}