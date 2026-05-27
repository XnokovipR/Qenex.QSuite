using System.Collections.ObjectModel;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Controls.Control;
using Telerik.Windows.Controls;
using Qenex.QSuite.Controls.GraphControl.ViewModels;
using Qenex.QSuite.Controls.SignalControl.Views;
using Qenex.QSuite.Controls.SignalControl.ViewModels;
using System.Windows.Media;
using Qenex.QInsight.Views;

namespace Qenex.QInsight.ViewModels;

public class ControlsViewModel : ViewModelBase
{
    #region  Fields

    private bool isUserControlLoaded;

    #endregion
    
    #region Constructors

    public ControlsViewModel(EventAggregator ea) : base(ea)
    {
        isUserControlLoaded = false;
        BackgroundColor = ShellWindow.TextEditorBackgroubndColor;
        Controls = new ObservableCollection<IControlBase>();
        UserControlLoadedCommand = new RelayCommand<RadDocking>(OnUserControlLoaded);
    }

    #endregion
    
    #region Properties
    
    public int LastControlId { get; set; } = 0;

    public ObservableCollection<IControlBase> Controls { get; set; }
    public RelayCommand<RadDocking> UserControlLoadedCommand { get; set; }
    public Color BackgroundColor { get; }
    
    #endregion

    #region ViewModelBase implementation

    public override string Header { get; set; } = "Controls";
    public override string Name { get; set; } = "ControlsViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Left;
    public override bool IsDocument => false;
    
    #endregion
    
    #region Commands methods

    private void OnUserControlLoaded(RadDocking radDocking)
    {
        if (isUserControlLoaded) return;
        
        var sc = new SignalControlViewModel();
        Controls.Add(sc);
        
        var rtgc = new GraphControlViewModel();
        Controls.Add(rtgc);
        
        isUserControlLoaded = true;
    }
    
    #endregion
}