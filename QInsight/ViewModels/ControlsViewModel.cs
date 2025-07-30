using Qenex.QLibs.QUI;

namespace Qenex.QInsight.ViewModels;

public class ControlsViewModel : ViewModelBase
{
    public ControlsViewModel(EventAggregator ea) : base(ea)
    {
        OnWindowLoadedCommand = new RelayCommand<object>(OnWindowLoaded);
    }
    
    #region Properties

    public RelayCommand<object> OnWindowLoadedCommand { get; set; }
    
    #endregion

    #region ViewModelBase implementation

    public override string Header { get; set; } = "Controls";
    public override string Name { get; set; } = "ControlsViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Left;
    public override bool IsDocument => false;
    
    public override void Exit()
    {
    }

    #endregion
    
    #region Commands methods

    private void OnWindowLoaded(object sfDiagram)
    {
        
    }
    #endregion
}