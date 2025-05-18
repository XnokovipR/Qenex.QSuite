using System.Windows.Media;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.TelerikDocking;
using Telerik.Windows.Controls.Docking;


namespace Qenex.QSuite.ViewModels;

public abstract class ViewModelBase(EventAggregator ea) : PropertyChangedBaseWithValidation, IBaseViewModel
{
    protected readonly EventAggregator EventAggregator = ea;
 
    #region Inherited from IBaseViewModel

    public abstract string Identifier { get; }
    public abstract string Header { get; set; }
    public abstract string Name { get; set; }
    public abstract DockingPosition DockPosition { get; set; }
    public abstract bool IsDocument { get; }
    public Dictionary<string, object> Tags { get; set; } = new();

    public abstract void Exit();

    #endregion
}