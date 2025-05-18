using System.Collections.ObjectModel;
using System.Windows;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.TelerikDocking;
using Qenex.QLibs.QUI.Wpf;

namespace Qenex.QSuite.ViewModels;

public class WorkspaceViewModel : ViewModelBase, IWorkspace
{
    #region Fields
    
    private bool diagramLoaded = false;

    #endregion

    #region Constructors

    public WorkspaceViewModel(EventAggregator ea,  string id = "") : base(ea)
    {
        Header = $"Workspace {id}";
        OnWindowLoadedCommand = new RelayCommand<object>(OnWindowLoaded);
    }

    #endregion
    
    #region Properties
    
    public RelayCommand<object> OnWindowLoadedCommand { get; set; }
   
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

    public override DockingPosition DockPosition { get; set; }

    public override bool IsDocument => true;

	public string WinTitle { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public override void Exit()
	{
	
	}

	#endregion


	#region Private methods

	private void OnWindowLoaded(object sfDiagram)
    {    
    }
    
    private static string GenerateWorkspaceIdentifier()
    {
        var workspaceName = $"WorkspaceViewModel_{Guid.NewGuid():N}";
        return workspaceName;
    }

	public bool Equals(WorkspaceViewModel other)
	{
		throw new NotImplementedException();
	}

	public void GotFocus(object sender, RoutedEventArgs e)
	{
		throw new NotImplementedException();
	}


	#endregion

}