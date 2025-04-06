using System.Collections.ObjectModel;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Models.Project;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.ViewModels.SolutionExplorerWrappers;
using Qenex.QSuite.ViewModels.ViewableItem;
using Syncfusion.Windows.Tools.Controls;

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

    public override DockSide DockingPosition { get => DockSide.Left; set { } }

    public override DockState DockState { get; set; }

    public override bool CanMaximize => false;
    public override bool IsDocument => false;
    public override bool CanClose => false;
    public override bool CanSerialize => true;

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
        // Add drivers
        CreateDriverWrappers(projectWrapper.Children, realPrjData.Module.Drivers);
        return projectWrapper;
    }
    
    private void CreateDriverWrappers(ObservableCollection<IViewableItem> children, IList<IDriverBase> drivers)
    {
        foreach (var driver in drivers)
        {
            var driverWrapper = new DriverWrapper(driver);
            // Add protocols
            CreateProtocolWrappers(driverWrapper.Children, driver.Protocols);
            children.Add(driverWrapper);
        }
    }
    
    private void CreateProtocolWrappers(ObservableCollection<IViewableItem> children, IList<IProtocolBase> protocols)
    {
        foreach (var protocol in protocols)
        {
            var protocolWrapper = new ProtocolWrapper(protocol);
            // Add variables
            CreateVariableWrappers(protocolWrapper.Children, protocol.Variables);
            children.Add(protocolWrapper);
        }
    }
    
    private void CreateVariableWrappers(ObservableCollection<IViewableItem> children, IList<IProtocolVariable> variables)
    {
        foreach (var variable in variables)
        {
            var variableWrapper = new ProtocolVariableWrapper(variable);
            children.Add(variableWrapper);
        }
    }

    #endregion
}