using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.DragDrop;
using Qenex.QInsight.Views;
using Telerik.Windows.Diagrams.Core;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Controls.GraphControl.ViewModels;
using Qenex.QSuite.LogSystems.LogSystem;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.ColorEditor.ColorSchemas;
using Telerik.Windows.Controls.Diagrams;
using Qenex.QSuite.Controls.SignalControl.ViewModels;
using Qenex.QSuite.Controls.SignalControl.Views;
using Qenex.QSuite.Controls.Control;
using Qenex.QSuite.Variables.QVariables;
using Telerik.Windows.DragDrop;


namespace Qenex.QInsight.ViewModels;

public class WorkspaceViewModel : WorkspaceViewModelBase
{
    #region  Fields
    
    private static readonly Brush DarkGridLineColor = new SolidColorBrush(Color.FromRgb(47,47,47));
    private static readonly Brush LightGridLineColor = new SolidColorBrush(Colors.WhiteSmoke);
    private static readonly Brush DarkBackgroundColor = new SolidColorBrush(Color.FromRgb(40, 40, 40));
    private static readonly Brush LightBackgroundColor = new SolidColorBrush(Colors.White);
    
    private bool isViewLoaded;

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

    public Brush BackgroundColor { get; set { field = value; OnPropertyChanged(); }}
    public Brush GridLineColor { get; set { field = value; OnPropertyChanged(); } }

    public bool IsGridVisible { get; set { field = value; OnPropertyChanged(); } } = true;
    public bool IsPageGridVisible { get; set { field = value; OnPropertyChanged(); } } = false;
    public double GridCellSize { get; set { field = value; OnPropertyChanged(); } } = 20;
    public bool IsSnapToGridEnabled { get; set { field = value; OnPropertyChanged(); } } = true;

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
         GridCellSize = Telerik.Windows.Controls.Diagrams.Primitives.BackgroundGrid.GetCellSize(diagram).Height;
	}

     public void AddControlToDiagram(IControlBase iControl, double x, double y)
     {
	     var viewTypeName = iControl.GetType().AssemblyQualifiedName?.Replace("Model", string.Empty);
	     if (viewTypeName == null)
	     {
		     return;
	     }
	     var viewType = Type.GetType(viewTypeName);

	     if (viewType == null)
	     {
		     return;
	     }
	     
	     var iControlView = (UserControl) Activator.CreateInstance(viewType)!;
	     iControlView.DataContext = iControl;
	     
	     AddControlToDiagram(iControl, iControlView, x, y);
     }

    private void AddControlToDiagram(IControlBase controlVm, UserControl control, double x, double y)
    {
	    var fgColor = ShellWindow.MainAppSettings.Design.AppTheme == ApplicationTheme.Dark ?
		    ShellWindow.MainAppSettings.Design.DarkThemeTextColor : ShellWindow.MainAppSettings.Design.LightThemeTextColor;
	    var bgColor = ShellWindow.MainAppSettings.Design.AppTheme == ApplicationTheme.Dark ? 
		    ShellWindow.MainAppSettings.Design.DarkThemeControlBackgroundColor : ShellWindow.MainAppSettings.Design.LightThemeControlBackgroundColor;
	    
		var userControl = new RadDiagramShape();
		controlVm.DiagramShape = userControl;
		controlVm.BackgroundColor = bgColor;
		controlVm.ForegroundColor = fgColor;
		controlVm.UpdateColorControl(bgColor, fgColor);
		
		userControl.Position = new Point(x, y);
		userControl.Width = controlVm.Width;
		userControl.Height = controlVm.Height;
		userControl.Content = control;
		userControl.Background = new SolidColorBrush(controlVm.BackgroundColor);
		userControl.BorderBrush = new SolidColorBrush(Colors.Black);
		userControl.BorderThickness = new Thickness(1);
		userControl.UseGlidingConnector = true;
		userControl.HorizontalContentAlignment = HorizontalAlignment.Stretch;
		userControl.VerticalContentAlignment = VerticalAlignment.Stretch;
		userControl.AllowDrop = true;

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

		if (controlVm.AreConnectorsEnabled)
		{
			userControl.Connectors.Add(leftCon);
			userControl.Connectors.Add(rightCon);
			userControl.Connectors.Add(topCon);
			userControl.Connectors.Add(bottomCon);
		}
		
		DragDropManager.AddPreviewDragOverHandler(userControl, OnVariableDragOver);
		DragDropManager.AddDragLeaveHandler(userControl, OnVariableDragLeave);
		DragDropManager.AddDropHandler(userControl, OnVariableDrop);

		diagram.AddShape(userControl);
	}
    
    private void OnVariableDragOver(object sender, Telerik.Windows.DragDrop.DragEventArgs e)
    {
	    var variable = DragDropPayloadManager.GetDataFromObject(e.Data, "DraggedVariable");
	    if (variable is IVariableBase)
	    {
		    VariableDragAndDropBehavior.IsOverValidTarget = true;
		    e.Effects = DragDropEffects.All;
		    e.Handled = true;
	    }
    }
    
    private void OnVariableDragLeave(object sender, Telerik.Windows.DragDrop.DragEventArgs e)
    {
	    VariableDragAndDropBehavior.IsOverValidTarget = false;
    }
    
    private void OnVariableDrop(object sender, Telerik.Windows.DragDrop.DragEventArgs e)
    {
	    var variable = DragDropPayloadManager.GetDataFromObject(e.Data, "DraggedVariable");
	    if (variable is not IVariableBase varBase) return;
    
	    if (sender is not RadDiagramShape shape) return;
	    if (shape.Content is not UserControl uc) return;
	    if (uc.DataContext is not IControlBase control) return;
	    
	    control.BindVariable(varBase);
	    VariableDragAndDropBehavior.IsOverValidTarget = false;
	    DragDropPayloadManager.SetData(e.Data, "ChosenControl", control);
	    e.Handled = true;
    }

	#endregion
}
