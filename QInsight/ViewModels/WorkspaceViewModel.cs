using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Qenex.QInsight.Views;
using Telerik.Windows.Diagrams.Core;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Controls.GraphControl.ViewModels;
using Qenex.QSuite.Controls.GraphControl.Views;
using Qenex.QSuite.LogSystems.LogSystem;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.ColorEditor.ColorSchemas;
using Telerik.Windows.Controls.Diagrams;


namespace Qenex.QInsight.ViewModels;

public class WorkspaceViewModel : WorkspaceViewModelBase
{
    #region  Fields
    
    private static readonly Brush DarkGridLineColor = new SolidColorBrush(Color.FromRgb(47,47,47));
    private static readonly Brush LightGridLineColor = new SolidColorBrush(Colors.WhiteSmoke);
    private static readonly Brush DarkBackgroundColor = new SolidColorBrush(Color.FromRgb(40, 40, 40));
    private static readonly Brush LightBackgroundColor = new SolidColorBrush(Colors.White);
    
    private bool isViewLoaded;
    private Brush gridLineColor = new SolidColorBrush(Colors.WhiteSmoke);
    private bool isGridVisible = true;
    private Brush backgroundColor = new SolidColorBrush(Colors.DimGray);
    private bool isPageGridVisible = false;

    #endregion
    
    #region Constructors

    public WorkspaceViewModel(EventAggregator ea) : base(ea)
    {
        GridLineColor = ShellWindow.IsDarkTheme ? DarkGridLineColor : LightGridLineColor;
        BackgroundColor = ShellWindow.IsDarkTheme ? DarkBackgroundColor : LightBackgroundColor;
        WorkspaceViewLoadedCommand = new RelayCommand<UserControl>(OnWorkspaceViewLoaded);
    }

    #endregion

    #region Properties

    public Brush BackgroundColor { get => backgroundColor; set { backgroundColor = value; OnPropertyChanged(); }}
    public Brush GridLineColor { get => gridLineColor; set { gridLineColor = value; OnPropertyChanged(); } }
    public bool IsGridVisible { get => isGridVisible; set { isGridVisible = value; OnPropertyChanged();} }
    public bool IsPageGridVisible { get => isPageGridVisible; set { isPageGridVisible = value; OnPropertyChanged(); } }

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
