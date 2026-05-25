using System.Collections.ObjectModel;
using System.Net.Mime;
using System.Windows;
using Qenex.QInsight.EventAggregatorMsgs;
using Qenex.QInsight.Models.Project;
using Qenex.QInsight.ViewModels.ModelWrappers;
using Qenex.QInsight.ViewModels.ViewableItem;
using Qenex.QLibs.QUI;
using Qenex.QInsight.ViewModels.SolutionExplorerWrappers;
using Qenex.QLibs.QUI.TelerikDocking;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Scripting.Script;
using Qenex.QSuite.Scripting.ScriptingEngine;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.ValuePresentation;
using Qenex.QSuite.Variables.VariableEvents;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.ViewModels;

public class SolutionExplorerViewModel : ViewModelBase
{
    
    #region Private fields

    private RealProjectData realProjectData = null!;

    #endregion

    #region Constructor

    public SolutionExplorerViewModel(EventAggregator ea) : base(ea)
    {
        EventAggregator.SubscribeAction<RemoveWorkspaceFromMainMenuMsg>(msg =>
        {
            var ws = Workspaces?.OfType<WorkspaceSeWrapper>().FirstOrDefault(ws => ws.Name == msg.Name);
            if (ws != null)
            {
                RemoveWorkspaceWrapper(ws);
            }
        });
        
        TreeViewDoubleClickCommand = new RelayCommand<IViewableItem>(treeViewItem =>
        {
            EventAggregator.Publish(new SolutionExplorerDoubleClickedItemMsg() { Item = treeViewItem });
        });
        
        TreeViewClickCommand = new RelayCommand<IViewableItem>(treeViewItem =>
        {
            EventAggregator.Publish(new SolutionExplorerClickedItemMsg() { Item = treeViewItem });
        });
        
        

        RemoveViewableItemCommand = new RelayCommand<IViewableItem>(item =>
        {
            RadWindow.Confirm(new DialogParameters()
            {
                Content = $"Do you want to remove workspace \"{item.Label}\"?",
                Header = "Remove Workspace",
                Owner = Application.Current.MainWindow,
                DialogStartupLocation = WindowStartupLocation.CenterOwner,
                Closed = (_, arg) =>
                {
                    if (arg.DialogResult != true) return;

                    if (item is WorkspaceSeWrapper wsw)
                    {
                        RemoveWorkspaceWrapper(item);
                        EventAggregator.Publish(new RemoveWorkspaceFromSolutionExplorerMsg() { Name = wsw.Name });
                    }
                }
            });
        });
        
        ProjectModules = [];
        EventAggregator.SubscribeAction<AddWorkspaceEaMsg>(msg =>
        {
            var childrens = ProjectModules.FirstOrDefault(p => p is ProjectSeWrapper)?.Children;
            if (childrens != null)
            {
                AddWorkspaceWrapper(childrens, msg.WorkspaceViewModel);
            }
        });
    }

    #endregion
    
    #region Treeview properties

    public ObservableCollection<IViewableItem> ProjectModules { get; set; }
    
    // Displayed workspaces in treeview
    public ObservableCollection<IViewableItem>? Workspaces { get; set; }
    
    public RelayCommand<IViewableItem> TreeViewDoubleClickCommand { get; set; }
    public RelayCommand<IViewableItem> TreeViewClickCommand { get; set; }
    public RelayCommand<IViewableItem> RemoveViewableItemCommand { get; set; }
    
    
    #endregion
    
    #region Actualize Solution treeview

    public void ReloadProjectData(RealProjectData realPrjData)
    {
        // Release all previous project data
        DisposeAll();
        
        realProjectData = realPrjData;
        var projectWrapper = CreateProjectWrapper(realPrjData);
        ProjectModules.Add(projectWrapper);
    }

    public void DisposeAll()
    {
        ProjectModules.Clear();
        Workspaces = null;
        realProjectData = null!;
    }

    #endregion
    
    #region Create All Project Wrappers

    private IViewableItem CreateProjectWrapper(RealProjectData realPrjData)
    {
        var projectWrapper = new ProjectSeWrapper(realPrjData.Module);
        
        CreateDriverWrappers(projectWrapper.Children, realPrjData.Module.Drivers);
        
        CreateVariableWrapper(projectWrapper.Children, realPrjData.Module.Variables);
        
        CreatePresentationWrapper(projectWrapper.Children, realPrjData.Module.Presentations);
        
        CreateVariableEventWrapper(projectWrapper.Children, realPrjData.Module.VarEvents);
        
        CreateScriptsWrapper(projectWrapper.Children, realPrjData.Module.Scripting);
        
        return projectWrapper;
    }
    
    private void CreateDriverWrappers(ObservableCollection<IViewableItem> children, IList<IDriverBase> drivers)
    {
        // Add drivers node
        var driversNode = new NodeSeWrapper(NodeSeWrapper.NodeType.Drivers, "Communicated");
        children.Add(driversNode);
        
        // Add drivers
        foreach (var driver in drivers)
        {
            var driverWrapper = new DriverSeWrapper(driver);
            // Add protocols
            CreateProtocolWrappers(driverWrapper.Children, driver.Protocols);
            driversNode.Children.Add(driverWrapper);
        }
    }
    
    private void CreateVariableWrapper(ObservableCollection<IViewableItem> children, IList<IVariableBase> variables)
    {
        // Add variables node
        var variablesNode = new NodeSeWrapper(NodeSeWrapper.NodeType.Variables);
        children.Add(variablesNode);
        
        // Add variables
        foreach (var variable in variables)
        {
            CreateVariableInNamespace(variablesNode.Children, variable);
        }
    }

    private void CreateVariableInNamespace(ObservableCollection<IViewableItem> variablesNode, IVariableBase variable)
    {
        var tempVariablesNode = variablesNode;
        var namespaces = variable.Namespace.Split('/').Skip(1).ToArray();
        
        if (namespaces.Length == 1 && namespaces[0] == "")
        {
            // Remove the first  empty namespace (variable is in root)
            namespaces = namespaces.Skip(1).ToArray();
        }

        foreach (var ns in namespaces)
        {
            // Check if the namespace already exists
            var existingNamespace = tempVariablesNode.FirstOrDefault(x => x.Label == ns);
            if (existingNamespace == null)
            {
                // Create a new namespace node
                var namespaceNode = new NodeSeWrapper(NodeSeWrapper.NodeType.OnlyPrefixFolder, ns);
                tempVariablesNode.Add(namespaceNode);
                tempVariablesNode = namespaceNode.Children;
            }
            else
            {
                // Use the existing namespace node
                tempVariablesNode = existingNamespace.Children;
            }
        }
        var variableWrapper = new VariableSeWrapper(variable);
        tempVariablesNode.Add(variableWrapper);
        
    }

    private void CreatePresentationWrapper(ObservableCollection<IViewableItem> children, IList<IPresentation> presentations)
    {
        // Add presentations node
        var presentationsNode = new NodeSeWrapper(NodeSeWrapper.NodeType.Presentations);
        children.Add(presentationsNode);
        
        // Add presentations
        foreach (var presentation in presentations)
        {
            var presentationWrapper = new PresentationSeWrapper(presentation);
            presentationsNode.Children.Add(presentationWrapper);
        }
    }
    
    private void CreateVariableEventWrapper(ObservableCollection<IViewableItem> children, IList<IVarEvent> events)
    {
        // Add events node
        var eventsNode = new NodeSeWrapper(NodeSeWrapper.NodeType.Events);
        children.Add(eventsNode);
        
        // Add events
        foreach (var variableEvent in events)
        {
            if (variableEvent is PeriodicVarEvent periodicVarEvent)
            {
                var periodicEventWrapper = new PeriodicVariableEventSeSeWrapper(periodicVarEvent);
                eventsNode.Children.Add(periodicEventWrapper);
            }
            else if (variableEvent is OnValueChangedVarEvent onChangeVarEvent)
            {
                var onChangeEventWrapper = new OnValueChangedVariableEventSeSeWrapper(onChangeVarEvent);
                eventsNode.Children.Add(onChangeEventWrapper);
            }
            else if (variableEvent is OnRequestVarEvent onRequestVarEvent)
            {
                var onTimeEventWrapper = new OnRequestVariableEventSeSeWrapper(onRequestVarEvent);
                eventsNode.Children.Add(onTimeEventWrapper);
            }
        }
    }
    
    private void CreateProtocolWrappers(ObservableCollection<IViewableItem> children, IList<IProtocolBase> protocols)
    {
        // Add protocols node
        var protocolsNode = new NodeSeWrapper(NodeSeWrapper.NodeType.Protocols, "Communicated");
        children.Add(protocolsNode);
        
        // Add protocols
        foreach (var protocol in protocols)
        {
            var protocolWrapper = new ProtocolSeWrapper(protocol);
            // Add variables
            CreateProtocolVariableWrappers(protocolWrapper.Children, protocol.Variables);
            protocolsNode.Children.Add(protocolWrapper);
        }
    }
    
    private void CreateProtocolVariableWrappers(ObservableCollection<IViewableItem> children, IList<IProtocolVariable> variables)
    {
        // Add variables node
        var variablesNode = new NodeSeWrapper(NodeSeWrapper.NodeType.Variables, "Communicated");
        children.Add(variablesNode);
        
        // Add variables
        foreach (var variable in variables)
        {
            var variableWrapper = new ProtocolVariableWrapper(variable);
            variablesNode.Children.Add(variableWrapper);
        }
    }
    
    public void AddWorkspace(IWorkspaceViewModel workspaceViewModel)
    {
        var projectChildren = ProjectModules.FirstOrDefault(p => p is ProjectSeWrapper)?.Children;
        if (projectChildren == null)
        {
            return;
        }

        AddWorkspaceWrapper(projectChildren, workspaceViewModel);
    }

    // Add workspace node
    private void AddWorkspaceWrapper(ObservableCollection<IViewableItem> children, IWorkspaceViewModel workspaceViewModel)
    {
        // Add workspaces node if not exists
        if (!children.Any(ch => ch is NodeSeWrapper { TypeOfNode: NodeSeWrapper.NodeType.Workspaces }))
        {
            var workspacesNode = new NodeSeWrapper(NodeSeWrapper.NodeType.Workspaces);
            Workspaces = workspacesNode.Children;
            children.Add(workspacesNode);   
            
        }
        
        // Add workspace
        var workspaces = children.First(ch => ch is NodeSeWrapper { TypeOfNode: NodeSeWrapper.NodeType.Workspaces });
        var workspaceWrapper = new WorkspaceSeWrapper(workspaceViewModel);
        workspaces.Children.Add(workspaceWrapper);
    }
    
    // Remove workspace node
    public void RemoveWorkspaceWrapper(IViewableItem workspaceItem)
    {
        var item = Workspaces.FirstOrDefault(i => i.Label == workspaceItem.Label);
        if (item != null)
        {
            Workspaces.Remove(item);
        }
    }

    private void CreateScriptsWrapper(ObservableCollection<IViewableItem> children, ScriptingContext scriptingContext)
    {
        // Add scripts node id does not exist
        if (!children.Any(ch => ch is NodeSeWrapper { TypeOfNode: NodeSeWrapper.NodeType.Scripts }))
        {
            var scriptsNode = new NodeSeWrapper(NodeSeWrapper.NodeType.Scripts);
            children.Add(scriptsNode);
        }

        // Add scripts
        var scrNode = children.First(ch => ch is NodeSeWrapper { TypeOfNode: NodeSeWrapper.NodeType.Scripts });
        foreach (var script in scriptingContext.Scripts)
        {
            var scriptWrapper = new ScriptWrapper(script, scriptingContext);
            var scriptSeWrapper = new ScriptSeWrapper(scriptWrapper);
            scrNode.Children.Add(scriptSeWrapper);
        }
    }

    #endregion
    
    #region ViewModelBase implementation

    public override string Header { get; set; } = "Solution Explorer";
    public override string Name { get; set; } = "SolutionExplorerViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Left;
    public override bool IsDocument => false;

    #endregion

}
