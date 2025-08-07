using System.IO;
using System.Windows;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.Models.Project;
using Qenex.QInsight.Views;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.Wpf;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.ViewModels;

public partial class ShellWindowModel
{
    #region Windows command Properties
   
    public RelayCommand<object> ShellWindowLocationChangedCommand { get; set; }
    public RelayCommand<object> ShellWindowSizeChangedCommand { get; set; }
    public RelayCommand<RadDocking> ShellWindowLoadedCommand { get; set; }
    public RelayCommand<RadDocking> ShellWindowClosingCommand { get; set; }

    #endregion

    #region Ribbon command Properties

    public RelayCommandAsync<RadDocking> RibbonOpenProjectCommand { get; set; }
    public RelayCommandAsync<RadDocking> RibbonCloseProjectCommand { get; set; }
    public RelayCommandAsync<RadDocking> RibbonAddWorkspaceCommand { get; set; }
    public RelayCommandAsync<RadDocking> RibbonRemoveWorkspaceCommand { get; set; }

    #endregion
    
    #region Create all ShellViewModel commands
    
    private void CreateCommands()
    {
        ShellWindowLoadedCommand = new RelayCommand<RadDocking>(OnShellWindowLoaded);
        ShellWindowClosingCommand = new RelayCommand<RadDocking>(OnShellWindowClosing);

        ShellWindowLocationChangedCommand = new RelayCommand<object>(OnShellWindowLocationChanged);
        ShellWindowSizeChangedCommand = new RelayCommand<object>(OnShellWindowSizeChanged);
        
        RibbonOpenProjectCommand = new RelayCommandAsync<RadDocking>(OpenProjectAsync);
        RibbonCloseProjectCommand = new RelayCommandAsync<RadDocking>(async (d) => await Task.CompletedTask);
        RibbonAddWorkspaceCommand = new RelayCommandAsync<RadDocking>(async (d) => await Task.CompletedTask);
        RibbonRemoveWorkspaceCommand = new RelayCommandAsync<RadDocking>(async (d) => await Task.CompletedTask);
    }

    #endregion
    
    #region Window command methods

    private void OnShellWindowLocationChanged(object obj)
    {
        if (Application.Current.MainWindow == null) return;

		ShellWindow.MainAppSettings.WinStyle.Top = Application.Current.MainWindow.Top;
		ShellWindow.MainAppSettings.WinStyle.Left = Application.Current.MainWindow.Left;
    }
    private void OnShellWindowSizeChanged(object obj)
    {
        if (Application.Current.MainWindow == null) return;

		ShellWindow.MainAppSettings.WinStyle.Height = (int)Application.Current.MainWindow.Height;
		ShellWindow.MainAppSettings.WinStyle.Width = (int) Application.Current.MainWindow.Width;
		ShellWindow.MainAppSettings.WinStyle.WinState = Application.Current.MainWindow.WindowState;
    }

    // This method is called when the docking manager is loaded
    private void OnShellWindowLoaded(RadDocking docking)
    {
        try
        {
            // Load drivers, protocols and controls
            pluginLoader = new PluginLoader(logger);
            driverPlugins = pluginLoader.GetPluginDetails<IDriverBase>("./Drivers");
            protocolPlugins = pluginLoader.GetPluginDetails<IProtocolBase>("./Protocols");
        }
        catch (Exception e)
        {
            eventAggregator.Publish(new LogMessage(LogLevel.Error, e.Message));
        }
    }

    // This method is called when the docking manager is closing
    private void OnShellWindowClosing(RadDocking docking)
    {
        try
        {
            AppSettings.SaveAppSettingsToFile("QInsightAppSettings.xml", ShellWindow.MainAppSettings);
        }
        catch (Exception e)
        {
            eventAggregator.Publish(new LogMessage(LogLevel.Error, e.Message));
        }
    }

	#endregion

	#region Ribbon command methods

    private async Task OpenProjectAsync(object obj)
    {
        var lastProjectPath = /*ShellWindow.MainAppSettings.LastProjectPath ??*/ Environment.CurrentDirectory;
        var dlg = new RadOpenFileDialog()
        {
            Owner = App.Current.MainWindow,
            Multiselect = false,
            Filter = "QInsight project files (*.zip)|*.zip",
            InitialDirectory = lastProjectPath
        };

        dlg.ShowDialog();

        
        if (dlg.DialogResult == true)
        {
            var filePath = dlg.FileName;
            //ShellWindow.MainAppSettings.LastProjectPath = Path.GetDirectoryName(dlg.FileName);
            try
            {
                var projectData = await ProjectZip.UnzipProjectFileAsync(filePath, logger);
                if (projectData == null)
                {
                    logger?.Log(LogLevel.Warn, $"Failed to load project file \"{Path.GetFileName(filePath)}\".");
                    return;
                }
                realProjectData = RealProjectData.CreateRealProjectData(driverPlugins, protocolPlugins, projectData, logger);
                
                solutionExplorerViewModel.ReloadProjectData(realProjectData);
                
                ChangeIsProjectMade(true);
                logger.Log(LogLevel.Info, $"Project file \"{Path.GetFileName(filePath)}\"loaded.");
                
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Error, e.Message);
            }
        }
    }

	#endregion
    
    #region Can-methods process

    private void ChangeIsProjectMade(bool isMade)
    {
        isProjectMade = isMade;
        //OnCloseProjectCommand?.OnCanExecuteChanged();
        //OnAddWorkspaceCommand?.OnCanExecuteChanged();
    }

    #endregion

}