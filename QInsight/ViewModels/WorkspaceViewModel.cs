using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Telerik.Windows.Diagrams.Core;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Controls.GraphControl.ViewModels;
using Qenex.QSuite.Controls.GraphControl.Views;
using Qenex.QSuite.LogSystems.LogSystem;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Diagrams;


namespace Qenex.QInsight.ViewModels;

public class WorkspaceViewModel : WorkspaceViewModelBase
{
    #region  Fields
    
    private bool isViewLoaded;

    #endregion
    
    #region Constructors

    public WorkspaceViewModel(EventAggregator ea) : base(ea)
    {
        WorkspaceViewLoadedCommand = new RelayCommand<UserControl>(OnWorkspaceViewLoaded);
    }

    #endregion

    #region Properties

    public RelayCommand<UserControl> WorkspaceViewLoadedCommand { get; set; }

    #endregion
    
    #region ViewModelBase implementation

    public override string Header { get; set; } = "Workspace";
    public override string Name { get; set; } = $"WorkspaceViewModel__{Guid.NewGuid().ToString().Replace("-", "_")}";
    public override string WinTitle { get; set; } = "Workspace" + Random.Shared.Next(1, 9999);
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Workspace;
    public override bool IsDocument => true;
    

    #endregion

    public override void OnIsVisibleChanged(bool isVisible)
    {
        //EventAggregator.Publish(new LogMessage(LogLevel.Info, $"{Name} visibility changed to {isVisible}"));
    }

    #region Commands methods

    private void OnWorkspaceViewLoaded(UserControl userControl)
    {
        if (isViewLoaded) return;
        isViewLoaded = true;
        
        // code here:
        
    }
    
    #endregion
}
