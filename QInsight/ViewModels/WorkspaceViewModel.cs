using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Telerik.Windows.Diagrams.Core;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Controls.GraphControl.ViewModels;
using Qenex.QSuite.Controls.GraphControl.Views;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Diagrams;


namespace Qenex.QInsight.ViewModels;

public class WorkspaceViewModel : ViewModelBase
{
    #region  Fields

    private bool isUserControlLoaded;
    private RadDiagram diagram;

    #endregion
    
    #region Constructors

    public WorkspaceViewModel(EventAggregator ea) : base(ea)
    {
        UserControlLoadedCommand = new RelayCommand<RadDiagram>(OnUserControlLoaded);
    }

    #endregion

    #region Properties

    public RelayCommand<RadDiagram> UserControlLoadedCommand { get; set; }

    #endregion
    
    #region ViewModelBase implementation

    public override string Header { get; set; } = "Workspace";
    public override string Name { get; set; } = "WorkspaceViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Workspace;
    public override bool IsDocument => true;

    #endregion
    
    #region Commands methods

    private void OnUserControlLoaded(RadDiagram radDiagram)
    {
        if (isUserControlLoaded) return;
     
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
        
        
        diagram.AddShape(userControl);
        
        isUserControlLoaded = true;
    }
    
    #endregion
}
