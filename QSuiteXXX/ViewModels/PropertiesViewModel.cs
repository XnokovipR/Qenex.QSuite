using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.TelerikDocking;
using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QSuite.ViewModels;

public class PropertiesViewModel : ViewModelBase
{
    #region Constructors

    public PropertiesViewModel(EventAggregator ea) : base(ea)
    {
    }

    #endregion
    
    #region Properties

    #region BaseViewModel implementation

    public override string Identifier => $"PropertiesViewModel:{Guid.NewGuid()}";
    public override string Header { get => "Properties"; set { } }
    public override string Name { get => "PropertiesViewModel"; set { } }
    public override DockingPosition DockPosition { get  => DockingPosition.Right; set { } }
	public override bool IsDocument => false;

	public override void Exit()
	{

	}

	#endregion

	#endregion
}