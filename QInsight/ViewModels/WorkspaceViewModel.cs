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
        //WorkspaceViewLoadedCommand = new RelayCommand<UserControl>(OnWorkspaceViewLoaded);
        WorkspaceViewLoadedCommand = new RelayCommand<RadDiagram>(OnWorkspaceViewLoaded);
    }

    #endregion

    #region Properties

    public Brush BackgroundColor { get => backgroundColor; set { backgroundColor = value; OnPropertyChanged(); }}
    public Brush GridLineColor { get => gridLineColor; set { gridLineColor = value; OnPropertyChanged(); } }
    public bool IsGridVisible { get => isGridVisible; set { isGridVisible = value; OnPropertyChanged();} }
    public bool IsPageGridVisible { get => isPageGridVisible; set { isPageGridVisible = value; OnPropertyChanged(); } }

    //public RelayCommand<UserControl> WorkspaceViewLoadedCommand { get; set; }
    public RelayCommand<RadDiagram> WorkspaceViewLoadedCommand { get; set; }

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

    // private void OnWorkspaceViewLoaded(UserControl userControl)
    // {
    //     if (isViewLoaded) return;
    //     isViewLoaded = true;
    //
    //     // code here:
    //
    //
    // }
    
    
     private RadDiagram diagram;
     private void OnWorkspaceViewLoaded(RadDiagram radDiagram)
     {
        if (isViewLoaded) return;
         isViewLoaded = true;
         diagram = radDiagram;
         var userControl = new RadDiagramShape();
         
         var graphVm = new GraphControlViewModel();
         var graphControl = new GraphControlView()
         {
             DataContext = graphVm,
         };
    
         userControl.Position = new Point(20, 150);
         userControl.Width = graphVm.Width;
         userControl.Height = graphVm.Height;
         userControl.Content = graphControl;
         userControl.Background = new SolidColorBrush(Colors.Blue);
         userControl.BorderBrush = new SolidColorBrush(Colors.DarkGray);
         userControl.BorderThickness = new Thickness(1);
         userControl.UseGlidingConnector = true;
         userControl.HorizontalContentAlignment = HorizontalAlignment.Stretch;
         userControl.VerticalContentAlignment = VerticalAlignment.Stretch;
         
         userControl.Connectors.Clear();
         var leftCon = new RadDiagramConnector()
         {
             Offset = new Point(0, 0.5),
             Name = "LeftConnector"
         };
         var rightCon = new RadDiagramConnector()
         {
             Offset = new Point(1, 0.5),
             Name = "RightConnector"
         };
         var topCon = new RadDiagramConnector()
         {
             Offset = new Point(0.5, 0),
             Name = "TopConnector"
         };
         var bottomCon = new RadDiagramConnector()
         {
             Offset = new Point(0.5, 1),
             Name = "BottomConnector"
         };
         
         
         userControl.Connectors.Add(leftCon);
         userControl.Connectors.Add(rightCon);
         userControl.Connectors.Add(topCon);
         userControl.Connectors.Add(bottomCon);
         // code here:
         
         
         diagram.AddShape(userControl);
         
     }
    
    #endregion
}
