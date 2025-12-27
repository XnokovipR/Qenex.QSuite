using System.Collections.ObjectModel;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Controls.Control;
using Telerik.Windows.Controls;
using Qenex.QSuite.Controls.GraphControl;
using Qenex.QSuite.Controls.GraphControl.ViewModels;
using Qenex.QSuite.Controls.SignalControl.Views;
using Qenex.QSuite.Controls.SignalControl.ViewModels;

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
        Controls = new ObservableCollection<IControlBase>();
        UserControlLoadedCommand = new RelayCommand<RadDocking>(OnUserControlLoaded);
    }

    #endregion
    
    #region Properties
    
    public int LastControlId { get; set; } = 0;

    public ObservableCollection<IControlBase> Controls { get; set; }
    public RelayCommand<RadDocking> UserControlLoadedCommand { get; set; }
    
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
        
        var gc = new GraphControlViewModel();
        Controls.Add(gc);

        var sc = new SignalControlViewModel();
        Controls.Add(sc);
        
        isUserControlLoaded = true;
    }
    
    #endregion
}