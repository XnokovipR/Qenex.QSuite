using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.TelerikDocking;
using Qenex.QSuite.Controls.Control;


namespace Qenex.QSuite.ViewModels;

public class ControlsViewModel : ViewModelBase
{
    #region Fields

    private bool viewLoaded = false;
    
    #endregion
    
    #region Constructors

    public ControlsViewModel(EventAggregator ea) : base(ea)
    {
        OnWindowLoadedCommand = new RelayCommand<object>(OnWindowLoaded);
    }

    #endregion
    
    #region Properties

    //public ObservableCollection<> Nodes { get; set; } = [];

    public RelayCommand<object> OnWindowLoadedCommand { get; set; }
    
    #endregion
    
    #region BaseViewModel implementation

    public override string Identifier => $"ControlsViewModel:{Guid.NewGuid().ToString()}";
	public override string Header { get => "Controls"; set { } }
    public override string Name { get => "SolutionExplorerViewModel"; set { } }
	public override DockingPosition DockPosition { get => DockingPosition.Right; set { } }
    public override bool IsDocument => false;
    
    public override void Exit()
    {
        throw new NotImplementedException();
    }

    #endregion
    
    #region Private methods

    private void OnWindowLoaded(object sfDiagram)
    {
        
    }
    #endregion
}

