using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.TelerikDocking;

namespace Qenex.QInsight.ViewModels;

public class WorkspacePropertiesViewModel : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    private IWorkspaceViewModel workspaceViewModel;
    private EventAggregator eventAggregator;
    private Action<string> update;
    
    public WorkspacePropertiesViewModel(EventAggregator ea, IWorkspaceViewModel workspaceVm, Action<string> updateAction)
    {
        eventAggregator = ea;
        update = updateAction;
        workspaceViewModel = workspaceVm;
    }
    
    public string WorkspaceTitle
    {
        get => workspaceViewModel.WinTitle;
        set
        {
            if (value == string.Empty) return;
            workspaceViewModel.WinTitle = value;
            OnPropertyChanged();
            update?.Invoke(value);
        }
    }
}