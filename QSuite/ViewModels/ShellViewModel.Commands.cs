using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using Qenex.QLibs.QUI;
using Qenex.QSuite.LogSystem;
using Syncfusion.Windows.Shared;
using Syncfusion.Windows.Tools.Controls;

namespace Qenex.QSuite.ViewModels;

public partial class ShellViewModel
{
    #region Properties

    #region Commands
    
    public RelayCommand<DockingManager> OnLoadedCommand { get; set; }
    public RelayCommand<DockingManager> OnClosingCommand { get; set; }

    #region Ribbon Commands

    public RelayCommand<DockingManager> OnSolutionExplorerCommand { get; set; }
    public RelayCommand<DockingManager> OnLogsCommand { get; set; }


    #endregion Ribbon Commands
    
    #endregion Commands
    #endregion Properties    
    
    private void CreateCommands()
    {
        OnLoadedCommand = new RelayCommand<DockingManager>(OnLoaded);
        OnClosingCommand = new RelayCommand<DockingManager>(OnClosing);

        OnSolutionExplorerCommand = new RelayCommand<DockingManager>( dm =>
        {
            var dockItem = DockingManager.DockableViewModels.FirstOrDefault(d => d.Name == "SolutionExplorerViewModel");
            if (dockItem != null)
            {
                dm.ExecuteRestore(dockItem.Content, DockState.Hidden);
            }
        });

        OnLogsCommand = new RelayCommand<DockingManager>( dm =>
        {
            var dockItem = DockingManager.DockableViewModels.FirstOrDefault(d => d.Name == "LogViewModel");
            if (dockItem != null)
            {
                dm.ExecuteRestore(dockItem.Content, DockState.Hidden);
            }
        });
    }

    private void OnLoaded(DockingManager docking)
    {
        if (!File.Exists("QenexEditLayout.xml")) return;
        
        try
        {
            docking.LoadDockState("QenexEditLayout.xml");
        }
        catch (Exception e)
        {
            eventAggregator.Publish(new LogMessage(LogLevel.Error, e.Message));
        }
    }

    private void OnClosing(DockingManager docking)
    {
        try
        {
            docking.SaveDockState("QenexEditLayout.xml");
        }
        catch (Exception e)
        {
            eventAggregator.Publish(new LogMessage(LogLevel.Error, e.Message));
        }
    }
}