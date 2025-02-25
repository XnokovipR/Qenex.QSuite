using System.Windows;
using Qenex.QLibs.QUI;
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

    #endregion

    #region Constructors

    public WorkspaceViewModel(EventAggregator ea) : base(ea)
    {
        DiagramLoadedCommand = new RelayCommand<SfDiagram>(OnDiagramLoaded);
    }

    #endregion
    
    #region Properties

    public string Identifier => GenerateWorkspaceIdentifier();
    public RelayCommand<SfDiagram> DiagramLoadedCommand { get; set; }
    public SfDiagram Diagram { get => diagram; set  { diagram = value; OnPropertyChanged(); }
}


    #endregion
    
    #region BaseViewModel implementation

    public override string Header
    {
        get => "Workspace";
        set { }
    }

    public override string Name
    {
        get => GenerateWorkspaceIdentifier();
        set { }
    }

    public override DockSide DockingPosition
    {
        get => DockSide.Top;
        set { }
    }

    public override bool IsDocument => true;
    public override bool CanClose => true;

    #endregion

    #region Public methods
    
    public void OnDiagramLoaded(SfDiagram sfDiagram)
    {
        diagram = sfDiagram;

        if (diagramLoaded) return;
        diagramLoaded = true;
        
    }

    #endregion

    #region Private methods

    private static string GenerateWorkspaceIdentifier()
    {
        var workspaceName = $"WorkspaceViewModel_{Guid.NewGuid():N}";
        return workspaceName;
    } 
    

    #endregion
    
}