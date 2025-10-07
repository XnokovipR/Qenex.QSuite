using System.IO;
using System.Windows;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.EventAggregatorMsgs;
using Qenex.QInsight.Models.Project;
using Qenex.QInsight.Views;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.TelerikDocking;
using Qenex.QLibs.QUI.Wpf;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.ViewModels;

public partial class ShellWindowModel
{
    private void SubscribeEventAggregatorMessages()
    {
        eventAggregator.SubscribeAction<SolutionTreeViewWorkspaceMsg>(OnSolutionTreeViewWorkspaceMsg);
    }

    private void OnSolutionTreeViewWorkspaceMsg(SolutionTreeViewWorkspaceMsg msg)
    {
        var workspaceVm = ViewModels.FirstOrDefault(vm => vm is IWorkspaceViewModel ws && ws.WinTitle.Equals(msg.Label));
        if (workspaceVm != null)
        {
            workspaceVm.IsHidden = !workspaceVm.IsHidden;
        }
    }
}