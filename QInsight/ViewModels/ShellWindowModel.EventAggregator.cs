using System.IO;
using System.Windows;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.EventAggregatorMsgs;
using Qenex.QInsight.Models.Project;
using Qenex.QInsight.ViewModels.ModelWrappers;
using Qenex.QInsight.ViewModels.SolutionExplorerWrappers;
using Qenex.QInsight.Views;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.TelerikDocking;
using Qenex.QLibs.QUI.Wpf;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.ViewModels;

public partial class ShellWindowModel
{
    private void SubscribeEventAggregatorMessages()
    {
        eventAggregator.SubscribeAction<SolutionExplorerClickedItemMsg>(OnSolutionExplorerClickedMsg);
        eventAggregator.SubscribeAction<SolutionExplorerDoubleClickedItemMsg>(OnSolutionExplorerDoubleClickedMsg);
        eventAggregator.SubscribeAction<RemoveWorkspaceFromSolutionExplorerMsg>(RemoveWorkspace);
        eventAggregator.SubscribeAction<ProjectConfigurationAppliedMsg>(_ => RefreshCommunicatedDriversProperties());
    }
    private void OnSolutionExplorerClickedMsg(SolutionExplorerClickedItemMsg msg)
    {
        var vm = ViewModels.FirstOrDefault(vm => vm.Name.Equals("PropertiesViewModel"));
        if (vm is not PropertiesViewModel propVm) return;
        
        switch (msg.Item)
        {
            case WorkspaceSeWrapper workspaceWrapper:
            {
                if (ViewModels.FirstOrDefault(viewModelBase => viewModelBase is IWorkspaceViewModel ws && ws.Name.Equals(workspaceWrapper.Name)) is IWorkspaceViewModel workspaceVm)
                {
                    propVm.SelectedViewModel = new WorkspacePropertiesViewModel(eventAggregator, workspaceVm);//, workspaceWrapper.Update);
                    ActivateOpenWorkspaceDocument(workspaceVm);
                }
                break;
            }
            case ScriptSeWrapper scriptSeWrapper:
            {
                propVm.SelectedViewModel = new ScriptPropertiesViewModel(eventAggregator, scriptSeWrapper.ScriptWrapper);
                var scriptViewModel = ViewModels
                    .OfType<ScriptViewModel>()
                    .FirstOrDefault(viewModel => ReferenceEquals(viewModel.ScriptWrapper.Script, scriptSeWrapper.ScriptWrapper.Script));
                if (scriptViewModel != null)
                {
                    ActivateOpenWorkspaceDocument(scriptViewModel);
                }

                break;
            }
            case DriverSeWrapper driverSeWrapper:
            {
                propVm.SelectedViewModel = new DriverPropertiesViewModel(eventAggregator, driverSeWrapper.Driver);
                break;
            }
            case ProtocolSeWrapper protocolSeWrapper:
            {
                propVm.SelectedViewModel = new ProtocolPropertiesViewModel(eventAggregator, protocolSeWrapper.Protocol);
                break;
            }
            case VariableSeWrapper variableSeWrapper:
            {
                propVm.SelectedViewModel = new VariablePropertiesViewModel(
                    eventAggregator,
                    variableSeWrapper,
                    realProjectData.Module.Presentations,
                    realProjectData.Module.VarEvents,
                    () => isEditVariableEnabled);
                break;
            }
            case ProtocolVariableWrapper protocolVariableWrapper:
            {
                propVm.SelectedViewModel = new ReadOnlyVariablePropertiesViewModel(
                    eventAggregator,
                    protocolVariableWrapper.ProtocolVariable);
                break;
            }
            case NodeSeWrapper { TypeOfNode: NodeSeWrapper.NodeType.Drivers } nodeWrapper
                when nodeWrapper.CustomTags?.TryGetValue("Drivers", out var drivers) == true
                     && drivers is IEnumerable<IDriverBase> communicatedDrivers:
            {
                propVm.SelectedViewModel = new CommunicatedDriversPropertiesViewModel(eventAggregator, communicatedDrivers);
                break;
            }
            case NodeSeWrapper { TypeOfNode: NodeSeWrapper.NodeType.Protocols } nodeWrapper
                when nodeWrapper.CustomTags?.TryGetValue("Protocols", out var protocols) == true
                     && protocols is IEnumerable<IProtocolBase> communicatedProtocols:
            {
                propVm.SelectedViewModel = new CommunicatedProtocolsPropertiesViewModel(eventAggregator, communicatedProtocols);
                break;
            }
            case NodeSeWrapper { TypeOfNode: NodeSeWrapper.NodeType.Variables } nodeWrapper
                when nodeWrapper.CustomTags?.TryGetValue("Variables", out var variables) == true
                     && variables is IList<IVariableBase> moduleVariables:
            {
                propVm.SelectedViewModel = new VariablesPropertiesViewModel(
                    eventAggregator,
                    moduleVariables,
                    () => isEditVariableEnabled,
                    value =>
                    {
                        isEditVariableEnabled = value;
                        if (propertiesViewModel.SelectedViewModel is VariablePropertiesViewModel variablePropertiesViewModel)
                        {
                            variablePropertiesViewModel.RefreshReadOnly();
                        }
                    });
                break;
            }
            default:
            {
                propVm.SelectedViewModel = new EmptyPropertiesViewModel(eventAggregator);
                break;
            }
        }
    }

    private void ActivateOpenWorkspaceDocument(IWorkspaceViewModel workspaceViewModel)
    {
        if (workspaceViewModel.IsHidden)
        {
            return;
        }

        var pane = FindDockingPaneForViewModel(shellRadDocking, workspaceViewModel);
        if (pane == null)
        {
            return;
        }

        shellRadDocking.ActivePane = pane;
        pane.IsActive = true;
        pane.Focus();
    }

    private void OnSolutionExplorerDoubleClickedMsg(SolutionExplorerDoubleClickedItemMsg msg)
    {
        switch (msg.Item)
        {
            case WorkspaceSeWrapper workspaceWrapper:
            {
                var workspaceVm = ViewModels.FirstOrDefault(vm => vm is IWorkspaceViewModel ws && ws.WinTitle.Equals(workspaceWrapper.Label));
                if (workspaceVm != null)
                {
                    workspaceVm.IsHidden = !workspaceVm.IsHidden;
                }
                break;
            }
            case ScriptSeWrapper scriptSeWrapper:
            {
                var foundScriptViewModel = ViewModels.FirstOrDefault(vm => vm is IWorkspaceViewModel ws && ws.WinTitle.Equals(scriptSeWrapper.Label));
                if (foundScriptViewModel != null)
                {
                    foundScriptViewModel.IsHidden = !foundScriptViewModel.IsHidden;
                }
                else
                {
                    var scriptViewModel = new ScriptViewModel(eventAggregator, scriptSeWrapper.ScriptWrapper);
                    ViewModels.Add(scriptViewModel);
                }
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
