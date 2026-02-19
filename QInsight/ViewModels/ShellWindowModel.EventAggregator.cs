using System.IO;
using System.Windows;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.EventAggregatorMsgs;
using Qenex.QInsight.Models.Project;
using Qenex.QInsight.ViewModels.SolutionExplorerWrappers;
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
        eventAggregator.SubscribeAction<SolutionExplorerClickedItemMsg>(OnSolutionExplorerClickedMsg);
        eventAggregator.SubscribeAction<SolutionExplorerDoubleClickedItemMsg>(OnSolutionExplorerDoubleClickedMsg);
        eventAggregator.SubscribeAction<RemoveWorkspaceFromSolutionExplorerMsg>(RemoveWorkspace);
    }
    private void OnSolutionExplorerClickedMsg(SolutionExplorerClickedItemMsg msg)
    {
        var vm = ViewModels.FirstOrDefault(vm => vm.Name.Equals("PropertiesViewModel"));
        if (vm is not PropertiesViewModel propVm) return;
        
        switch (msg.Item)
        {
            case WorkspaceWrapper workspaceWrapper:
            {
                if (ViewModels.FirstOrDefault(viewModelBase => viewModelBase is IWorkspaceViewModel ws && ws.WinTitle.Equals(workspaceWrapper.Label)) is IWorkspaceViewModel workspaceVm)
                {
                    propVm.SelectedViewModel = new WorkspacePropertiesViewModel(eventAggregator, workspaceVm, workspaceWrapper.Update);
                }
                break;
            }
            case ScriptWrapper scriptWrapper:
            {
                propVm.SelectedViewModel = new ScriptPropertiesViewModel(eventAggregator, scriptWrapper.Script);
                break;
            }
            default:
            {
                propVm.SelectedViewModel = new EmptyPropertiesViewModel(eventAggregator);
                break;
            }
        }
    }

    private void OnSolutionExplorerDoubleClickedMsg(SolutionExplorerDoubleClickedItemMsg msg)
    {
        switch (msg.Item)
        {
            case WorkspaceWrapper workspaceWrapper:
            {
                var workspaceVm = ViewModels.FirstOrDefault(vm => vm is IWorkspaceViewModel ws && ws.WinTitle.Equals(workspaceWrapper.Label));
                if (workspaceVm != null)
                {
                    workspaceVm.IsHidden = !workspaceVm.IsHidden;
                }
                break;
            }
            case ScriptWrapper scriptWrapper:
            {
                var script = scriptWrapper.Script;
                var scriptViewModel = new ScriptViewModel(eventAggregator, script);
                ViewModels.Add(scriptViewModel);
                break;
            }
        }
    }
    
    private void RemoveWorkspace(RemoveWorkspaceFromSolutionExplorerMsg msg)
    {
        var radGroup = shellRadDocking.SplitItems.OfType<RadPaneGroup>().FirstOrDefault(i => i.Name.Contains("WorkspacePaneGroup"));
        
        if (radGroup != null)
        {
            var panetoRemove = radGroup.Items.OfType<QRadDocumentPane>().FirstOrDefault(p => p.Name == msg.Name);
            if (panetoRemove != null)
            {
                panetoRemove.RemoveFromParent();
            }
        }
    }
}