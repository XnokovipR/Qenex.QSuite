using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.TelerikDocking;

namespace Qenex.QInsight.ViewModels;

public class WorkspacePropertiesViewModel(EventAggregator ea, IWorkspaceViewModel workspaceVm) : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    public string WorkspaceTitle
    {
        get => workspaceVm.WinTitle;
        set
        {
            if (value == string.Empty) return;
            workspaceVm.WinTitle = value;
            OnPropertyChanged();
        }
    }
}