using System.Windows;
using Qenex.QLibs.QUI;
using Syncfusion.UI.Xaml.Diagram;
using Syncfusion.Windows.Tools.Controls;

namespace Qenex.QSuite.ViewModels;

public class WorkspaceViewModel : ViewModelBase
{
    #region Fields

    #endregion

    #region Constructors

    public WorkspaceViewModel(EventAggregator ea) : base(ea)
    {
        //Diagram = new SfDiagram();
        DiagramLoadedCommand = new RelayCommand<SfDiagram>(OnDiagramLoaded);
    }

    #endregion
    
    #region Properties
 
    public RelayCommand<SfDiagram> DiagramLoadedCommand { get; set; }
    
    #endregion
    
    #region BaseViewModel implementation

    public override string Header
    {
        get => "Workspace";
        set { }
    }

    public override string Name
    {
        get => "WorkspaceViewModel";
        set { }
    }

    public override DockSide DockingPosition
    {
        get => DockSide.Top;
        set { }
    }

    public override bool IsDocument => true;
    public override bool CanClose => false;

    #endregion   
    
    public void OnDiagramLoaded(SfDiagram d)
    {
        EventAggregator.Publish(d);
    }
}