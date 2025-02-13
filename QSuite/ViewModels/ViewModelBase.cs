using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.SyncfusionDocking;
using Syncfusion.Windows.Tools.Controls;

namespace Qenex.QSuite.ViewModels;

public abstract class ViewModelBase(EventAggregator ea) : PropertyChangedBaseWithValidation, IBaseViewModel
{
    protected EventAggregator EventAggregator = ea;
 
    #region Inherited from IBaseViewModel

    public abstract string Header { get; set; }
    public abstract string Name { get; set; }
    public abstract DockSide DockingPosition { get; set; }
    public abstract bool IsDocument { get; }
    public abstract bool CanClose { get; }

    public virtual void Exit() { }

    #endregion    
}