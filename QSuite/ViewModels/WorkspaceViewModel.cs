using System.Collections.ObjectModel;
using System.Windows;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.SyncfusionDocking;
using Qenex.QLibs.QUI.Wpf;
using Syncfusion.UI.Xaml.Diagram;
using Syncfusion.UI.Xaml.Diagram.Controls;
using Syncfusion.Windows.Tools.Controls;

namespace Qenex.QSuite.ViewModels;

public class WorkspaceViewModel : ViewModelBase
{
    #region Fields
    
    private bool diagramLoaded = false;
    SfDiagram diagram;
    private DockingManager dockingManager;

    #endregion

    #region Constructors

    public WorkspaceViewModel(EventAggregator ea, DockingManager dm,  string id = "") : base(ea)
    {
        dockingManager = dm;
        Header = $"Workspace {id}";
        OnWindowLoadedCommand = new RelayCommand<object>(OnWindowLoaded);
    }

    #endregion
    
    #region Properties
    
    public RelayCommand<object> OnWindowLoadedCommand { get; set; }
    public SfDiagram Diagram { get => diagram; set  { diagram = value; OnPropertyChanged(); } }

    public ObservableCollection<Node> Controls { get; set; } = [];
    
    #endregion
    
    #region BaseViewModel implementation

    public override string Identifier => GenerateWorkspaceIdentifier();

    private string header;
    public override string Header
    {
        get => header;
        set { header = value; OnPropertyChanged(); }
    }

    public override string Name
    {
        get => GenerateWorkspaceIdentifier();
        set { }
    }

    public override DockSide DockingPosition { get; set; } 
    
    public override DockState DockState { get; set; } = DockState.Document;

    public override bool CanMaximize => true;
    public override bool IsDocument => true;
    public override bool CanClose => true;
    public override bool CanSerialize => false;
    

    #endregion
    

    #region Private methods

    private void OnWindowLoaded(object sfDiagram)
    {
        //diagram = sfDiagram;

        if (diagramLoaded) return;
        diagramLoaded = true;
        
        // Set logic for diagram loaded
        var aa = dockingManager.Children.Count;
        
    }
    
    private static string GenerateWorkspaceIdentifier()
    {
        var workspaceName = $"WorkspaceViewModel_{Guid.NewGuid():N}";
        return workspaceName;
    } 
    

    #endregion
    
}