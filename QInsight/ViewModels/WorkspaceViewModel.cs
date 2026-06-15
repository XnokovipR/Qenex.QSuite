using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.DragDrop;
using Qenex.QInsight.EventAggregatorMsgs;
using Qenex.QInsight.Views;
using Telerik.Windows.Diagrams.Core;
using Qenex.QLibs.QUI;
using Qenex.QSuite.LogSystems.LogSystem;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.ColorEditor.ColorSchemas;
using Telerik.Windows.Controls.Diagrams;
using Qenex.QSuite.Controls.Control;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using Telerik.Windows.DragDrop;
using Telerik.Windows.Controls.FileDialogs;

namespace Qenex.QInsight.ViewModels;

public class WorkspaceViewModel : WorkspaceViewModelBase
{
    #region  Fields
    
    private static readonly Brush DarkGridLineColor = new SolidColorBrush(Color.FromRgb(47,47,47));
    private static readonly Brush LightGridLineColor = new SolidColorBrush(Colors.WhiteSmoke);
    private static readonly Brush DarkBackgroundColor = new SolidColorBrush(Color.FromRgb(40, 40, 40));
    private static readonly Brush LightBackgroundColor = new SolidColorBrush(Colors.White);
    
    private bool isViewLoaded;
    private List<ControlBase> controlsToLoad = [];
    private List<IProtocolVariable> activeProtocolVariables = [];
    private readonly List<(IProtocolVariable ProtocolVariable, Func<IProtocolVariable, Task> Handler)> loadedVariableSubscriptions = [];

    #endregion
    
    #region Constructors

    public WorkspaceViewModel(EventAggregator ea) : base(ea)
    {
        GridLineColor = ShellWindow.IsDarkTheme ? DarkGridLineColor : LightGridLineColor;
        BackgroundColor = ShellWindow.IsDarkTheme ? DarkBackgroundColor : LightBackgroundColor;
        //WorkspaceViewLoadedCommand = new RelayCommand<UserControl>(OnWorkspaceViewLoaded);
        WorkspaceViewLoadedCommand = new RelayCommand<RadDiagram>(OnWorkspaceViewLoaded);
        EventAggregator.SubscribeAction<VariablePropertiesChangedMsg>(msg =>
        {
	        RefreshControlVariableBindings(msg.Variable);
        });
        EventAggregator.SubscribeAction<VariableUsageQuery>(CollectVariableUsage);
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

    public Action<DialogWindowBase>? ConfigureGraphControlSaveDialog { get; set; }
    public Func<string?>? GraphControlSaveDialogInitialDirectoryProvider { get; set; }
    public Action<string>? GraphControlSaveDialogDirectoryChanged { get; set; }

    #endregion
    
    #region ViewModelBase implementation

    public override string Header { get; set; } = "Workspace";
    public override string Name { get; set; } = $"WorkspaceViewModel_WsControls_{Guid.NewGuid().ToString().Replace("-", "_")}";
    public override string WinTitle { get; set { field = value; OnPropertyChanged(); } } = "Workspace" + Random.Shared.Next(1, 9999);
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Workspace;
    public override bool IsDocument => true;
    

    #endregion

    public override void OnIsVisibleChanged(bool isVisible)
    {
        //EventAggregator.Publish(new LogMessage(LogLevel.Info, $"{Name} visibility changed to {isVisible}"));
    }

	#region Commands methods

	private RadDiagram diagram;
	 private void OnWorkspaceViewLoaded(RadDiagram radDiagram)
	 {
        if (isViewLoaded && ReferenceEquals(diagram, radDiagram)) return;
        var controlsToRestore = isViewLoaded
	        ? GetControlProjectData()
	        : controlsToLoad.ToList();

         isViewLoaded = true;
         diagram = radDiagram;
         GridCellSize = Telerik.Windows.Controls.Diagrams.Primitives.BackgroundGrid.GetCellSize(diagram).Height;

         var diagramAlreadyContainsControls = GetDiagramControls(diagram).Any();
         if (!diagramAlreadyContainsControls)
         {
	         foreach (var controlData in controlsToRestore)
	         {
		         AddControlToDiagram(controlData);
	         }
         }

         controlsToLoad.Clear();

         if (activeProtocolVariables.Count > 0)
         {
	         BindLoadedControlVariables(activeProtocolVariables);
         }
	}

     public List<ControlBase> GetControlProjectData()
     {
	     if (!isViewLoaded)
	     {
		     foreach (var control in controlsToLoad)
		     {
			     SynchronizeSavedVariableBindings(control);
		     }

		     return controlsToLoad.ToList();
	     }

	     return GetDiagramControls()
		     .Select(item =>
		     {
			     item.Control.X = (int)Math.Round(item.Shape.Position.X);
			     item.Control.Y = (int)Math.Round(item.Shape.Position.Y);
			     item.Control.Width = (int)Math.Round(item.Shape.Width);
			     item.Control.Height = (int)Math.Round(item.Shape.Height);
			     SynchronizeSavedVariableBindings(item.Control);
			     return item.Control;
		     })
		     .ToList();
     }

     public void SetControlProjectData(IEnumerable<ControlBase> controls)
     {
	     controlsToLoad = controls.ToList();

	     if (!isViewLoaded)
	     {
		     return;
	     }

	     foreach (var controlData in controlsToLoad)
	     {
		     AddControlToDiagram(controlData);
	     }

	     controlsToLoad.Clear();
     }

     public void AddControlToDiagram(IControlBase iControl, double x, double y)
     {
	     var controlType = iControl.GetType();
	     var viewTypeName = controlType.AssemblyQualifiedName?.Replace("Model", string.Empty);
	     if (viewTypeName == null)
	     {
		     return;
	     }

	     // Plugin controls (nactene pres Assembly.LoadFrom) nemusi byt dohledatelne pres Type.GetType,
	     // proto fallback na assembly daneho controlu (View je ve stejne assembly jako ViewModel).
	     var viewType = Type.GetType(viewTypeName)
		     ?? controlType.Assembly.GetType(controlType.FullName!.Replace("Model", string.Empty));

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
	    ConfigureControl(controlVm);

	    var fgColor = ShellWindow.ForegroundColor;
	    var bgColor = ShellWindow.BackgroundColor;
	    var fontSize = ShellWindow.MainAppSettings.Design.FontSize;
	    
		var userControl = new RadDiagramShape();
		controlVm.DiagramShape = userControl;
		controlVm.BackgroundColor = bgColor;
		controlVm.ForegroundColor = fgColor;
		controlVm.UpdateThemeSettingsControl(bgColor, fgColor, fontSize > 0 ? fontSize : 12);
		controlVm.X = (int)Math.Round(x);
		controlVm.Y = (int)Math.Round(y);
		
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
		userControl.IsManipulationEnabled = !controlVm.IsLocked;
		userControl.IsManipulationAdornerVisible = !controlVm.IsLocked;
		userControl.IsManipulationAdornerVisible = !controlVm.IsLocked;
		userControl.IsDraggingEnabled = !controlVm.IsLocked;
		userControl.IsRotationEnabled = !controlVm.IsLocked;
		userControl.IsResizingEnabled = !controlVm.IsLocked;

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

    private void ConfigureControl(IControlBase controlVm)
    {
	    if (controlVm is not IFileDialogAwareControl dialogAwareControl)
	    {
		    return;
	    }

	    dialogAwareControl.ConfigureSaveFileDialog = ConfigureGraphControlSaveDialog;
	    dialogAwareControl.SaveDialogInitialDirectoryProvider = GraphControlSaveDialogInitialDirectoryProvider;
	    dialogAwareControl.SaveDialogDirectoryChanged = GraphControlSaveDialogDirectoryChanged;
    }

    public void BindLoadedControlVariables(IEnumerable<IProtocolVariable> protocolVariables)
    {
	    UnsubscribeLoadedControlVariables();
	    activeProtocolVariables = protocolVariables.ToList();
	    var projectProtocolVariables = activeProtocolVariables;
	    var controls = GetWorkspaceControls();

	    foreach (var control in controls)
	    {
		    foreach (var variableReference in GetControlVariableReferences(control))
		    {
			    var protocolVariable = projectProtocolVariables.FirstOrDefault(v =>
				    ControlBase.IsVariableReferenceMatch(variableReference, v.Variable));
			    if (protocolVariable != null)
			    {
				    control.BindVariable(protocolVariable.Variable);
				    Func<IProtocolVariable, Task> handler = changedProtocolVariable =>
				    {
					    var variableSnapshot = CreateVariableSnapshot(changedProtocolVariable.Variable);
					    if (Application.Current?.Dispatcher == null || Application.Current.Dispatcher.CheckAccess())
					    {
						    return control.UpdateVariableValueAsync(variableSnapshot);
					    }

					    return Application.Current.Dispatcher
						    .InvokeAsync(() => control.UpdateVariableValueAsync(variableSnapshot))
						    .Task
						    .Unwrap();
				    };

				    protocolVariable.SubscribeAsyncValueChanged(handler);
				    loadedVariableSubscriptions.Add((protocolVariable, handler));
			    }
		    }
	    }
    }

    public void SetControlsRunState(bool isRun)
    {
	    foreach (var control in GetWorkspaceControls())
	    {
		    control.IsRun = isRun;
	    }
    }

    private static IVariableBase CreateVariableSnapshot(IVariableBase variable)
    {
	    return variable switch
	    {
		    ScalarVariable scalarVariable => CreateScalarVariableSnapshot(scalarVariable),
		    StringVariable stringVariable => CreateStringVariableSnapshot(stringVariable),
		    _ => variable
	    };
    }

    private static ScalarVariable CreateScalarVariableSnapshot(ScalarVariable variable)
    {
	    var values = ValuesGlobal.CreateInstance(variable.Values.ValueType);
	    values.ValPresentation = variable.Values.ValPresentation;
	    values.SetValue(variable.GetValue());

	    return new ScalarVariable
	    {
		    Id = variable.Id,
		    Namespace = variable.Namespace,
		    Name = variable.Name,
		    Label = variable.Label,
		    Description = variable.Description,
		    Timestamp = variable.Timestamp,
		    CommComponents = variable.CommComponents,
		    Size = variable.Size,
		    Values = values
	    };
    }

    private static StringVariable CreateStringVariableSnapshot(StringVariable variable)
    {
	    return new StringVariable
	    {
		    Id = variable.Id,
		    Namespace = variable.Namespace,
		    Name = variable.Name,
		    Label = variable.Label,
		    Description = variable.Description,
		    Timestamp = variable.Timestamp,
		    CommComponents = variable.CommComponents,
		    Values = variable.Values
	    };
    }

    public override void Clean()
    {
	    UnsubscribeLoadedControlVariables();
	    activeProtocolVariables.Clear();
    }

    private void RefreshControlVariableBindings(IVariableBase variable)
    {
	    foreach (var control in GetWorkspaceControls())
	    {
		    control.RefreshVariableBinding(variable);
		    SynchronizeSavedVariableBindings(control);
	    }
    }

    private void CollectVariableUsage(VariableUsageQuery query)
    {
	    foreach (var control in GetWorkspaceControls())
	    {
		    if (control.IsVariableUsed(query.Variable))
		    {
			    query.Usages.Add(new VariableUsage
			    {
				    WorkspaceName = WinTitle,
				    ControlLabel = $"{control.Label} (Id {control.Id})"
			    });
		    }
	    }
    }

    public override Task CleanAsync(CancellationToken ct = default)
    {
	    UnsubscribeLoadedControlVariables();
	    activeProtocolVariables.Clear();
	    return Task.CompletedTask;
    }

    private void AddControlToDiagram(ControlBase control)
    {
	    AddControlToDiagram(control, control.X, control.Y);
    }

    private IEnumerable<(RadDiagramShape Shape, ControlBase Control)> GetDiagramControls()
    {
	    return GetDiagramControls(diagram);
    }

    private static IEnumerable<(RadDiagramShape Shape, ControlBase Control)> GetDiagramControls(RadDiagram sourceDiagram)
    {
	    return sourceDiagram
		    .Shapes
		    .OfType<RadDiagramShape>()
		    .Select(shape => (Shape: shape, Control: (shape.Content as UserControl)?.DataContext as ControlBase))
		    .Where(item => item.Control != null)!;
    }

    private IEnumerable<ControlBase> GetWorkspaceControls()
    {
	    if (!isViewLoaded)
	    {
		    return controlsToLoad;
	    }

	    return controlsToLoad
		    .Concat(GetDiagramControls().Select(item => item.Control))
		    .Distinct();
    }

    private static IEnumerable<string> GetControlVariableReferences(ControlBase control)
    {
	    var references = control.LinkedVariables.ToList();
	    references.AddRange(control.Variables.Select(ControlBase.GetVariableReference));

	    if (control is IVariableReferenceProvider referenceProvider)
	    {
		    references.AddRange(referenceProvider.GetAdditionalVariableReferences());
	    }

	    return references
		    .Where(reference => !string.IsNullOrWhiteSpace(reference))
		    .Distinct();
    }

    private static void SynchronizeSavedVariableBindings(ControlBase control)
    {
	    foreach (var variable in control.Variables)
	    {
		    control.RememberVariableBinding(variable);
	    }
    }

    private void UnsubscribeLoadedControlVariables()
    {
	    foreach (var subscription in loadedVariableSubscriptions)
	    {
		    subscription.ProtocolVariable.UnsubscribeAsyncValueChanged(subscription.Handler);
	    }

	    loadedVariableSubscriptions.Clear();
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
