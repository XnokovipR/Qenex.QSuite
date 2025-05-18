using System.Collections.ObjectModel;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.TelerikDocking;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Models.Project;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.ValuePresentation;
using Qenex.QSuite.Variables.VariableEvents;
using Qenex.QSuite.ViewModels.SolutionExplorerWrappers;
using Qenex.QSuite.ViewModels.ViewableItem;

namespace Qenex.QSuite.ViewModels;

public class SolutionExplorerViewModel(EventAggregator ea) : ViewModelBase(ea)
{
    #region Private fields

    private RealProjectData realProjectData = null!;

    #endregion
    
    #region TreeView properties

    public ObservableCollection<IViewableItem> ProjectModules { get; set; } = [];

    #endregion

    #region BaseViewModel Properties

    public override string Identifier => $"SolutionExplorerViewModel:{Guid.NewGuid().ToString()}";

    public override string Header { get => "Solution Explorer"; set { } }

    public override string Name { get => "SolutionExplorerViewModel"; set { } }

    public override DockingPosition DockPosition { get => DockingPosition.Left; set { } }

    public override bool IsDocument => false;

	public override void Exit()
	{
	
	}

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
        realProjectData = null!;
    }

    #endregion

    #region Create All Project Wrappers

    private IViewableItem CreateProjectWrapper(RealProjectData realPrjData)
    {
        var projectWrapper = new ProjectWrapper(realPrjData.Module);
        
        CreateDriverWrappers(projectWrapper.Children, realPrjData.Module.Drivers);
        
        CreateVariableWrapper(projectWrapper.Children, realPrjData.Module.Variables);
        
        CreatePresentationWrapper(projectWrapper.Children, realPrjData.Module.Presentations);
        
        CreateVariableEventWrapper(projectWrapper.Children, realPrjData.Module.VarEvents);
        return projectWrapper;
    }
    
    private void CreateDriverWrappers(ObservableCollection<IViewableItem> children, IList<IDriverBase> drivers)
    {
        // Add drivers node
        var driversNode = new NodeWrapper(NodeWrapper.NodeType.Drivers, "Communicated");
        children.Add(driversNode);
        
        // Add drivers
        foreach (var driver in drivers)
        {
            var driverWrapper = new DriverWrapper(driver);
            // Add protocols
            CreateProtocolWrappers(driverWrapper.Children, driver.Protocols);
            driversNode.Children.Add(driverWrapper);
        }
    }
    
    private void CreateVariableWrapper(ObservableCollection<IViewableItem> children, IList<IVariableBase> variables)
    {
        // Add variables node
        var variablesNode = new NodeWrapper(NodeWrapper.NodeType.Variables);
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
                var namespaceNode = new NodeWrapper(NodeWrapper.NodeType.OnlyPrefixFolder, ns);
                tempVariablesNode.Add(namespaceNode);
                tempVariablesNode = namespaceNode.Children;
            }
            else
            {
                // Use the existing namespace node
                tempVariablesNode = existingNamespace.Children;
            }
        }
        var variableWrapper = new VariableWrapper(variable);
        tempVariablesNode.Add(variableWrapper);
        
    }

    private void CreatePresentationWrapper(ObservableCollection<IViewableItem> children, IList<IPresentation> presentations)
    {
        // Add presentations node
        var presentationsNode = new NodeWrapper(NodeWrapper.NodeType.Presentations);
        children.Add(presentationsNode);
        
        // Add presentations
        foreach (var presentation in presentations)
        {
            var presentationWrapper = new PresentationWrapper(presentation);
            presentationsNode.Children.Add(presentationWrapper);
        }
    }
    
    private void CreateVariableEventWrapper(ObservableCollection<IViewableItem> children, IList<IVarEvent> events)
    {
        // Add events node
        var eventsNode = new NodeWrapper(NodeWrapper.NodeType.Events);
        children.Add(eventsNode);
        
        // Add events
        foreach (var variableEvent in events)
        {
            if (variableEvent is PeriodicVarEvent periodicVarEvent)
            {
                var periodicEventWrapper = new PeriodicVariableEventWrapper(periodicVarEvent);
                eventsNode.Children.Add(periodicEventWrapper);
            }
            else if (variableEvent is OnValueChangedVarEvent onChangeVarEvent)
            {
                var onChangeEventWrapper = new OnValueChangedVariableEventWrapper(onChangeVarEvent);
                eventsNode.Children.Add(onChangeEventWrapper);
            }
            else if (variableEvent is OnRequestVarEvent onRequestVarEvent)
            {
                var onTimeEventWrapper = new OnRequestVariableEventWrapper(onRequestVarEvent);
                eventsNode.Children.Add(onTimeEventWrapper);
            }
        }
    }
    
    private void CreateProtocolWrappers(ObservableCollection<IViewableItem> children, IList<IProtocolBase> protocols)
    {
        // Add protocols node
        var protocolsNode = new NodeWrapper(NodeWrapper.NodeType.Protocols, "Communicated");
        children.Add(protocolsNode);
        
        // Add protocols
        foreach (var protocol in protocols)
        {
            var protocolWrapper = new ProtocolWrapper(protocol);
            // Add variables
            CreateProtocolVariableWrappers(protocolWrapper.Children, protocol.Variables);
            protocolsNode.Children.Add(protocolWrapper);
        }
    }
    
    private void CreateProtocolVariableWrappers(ObservableCollection<IViewableItem> children, IList<IProtocolVariable> variables)
    {
        // Add variables node
        var variablesNode = new NodeWrapper(NodeWrapper.NodeType.Variables, "Communicated");
        children.Add(variablesNode);
        
        // Add variables
        foreach (var variable in variables)
        {
            var variableWrapper = new ProtocolVariableWrapper(variable);
            variablesNode.Children.Add(variableWrapper);
        }
    }

    #endregion
}
