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
using Telerik.Windows.Controls.Docking;

namespace Qenex.QInsight.ViewModels;

public partial class ShellWindowModel
{
    #region Windows command Properties
   
    public RelayCommand<object> ShellWindowLocationChangedCommand { get; set; }
    public RelayCommand<object> ShellWindowSizeChangedCommand { get; set; }
    public RelayCommandAsync<RadDocking> ShellWindowLoadedCommandAsync { get; set; }
    public RelayCommand<RadDocking> ShellWindowClosingCommand { get; set; }
    
    public RelayCommandAsync<StateChangeEventArgs> PanelCloseCommandAsync { get; set; }


    #endregion

    #region Ribbon command Properties

    public RelayCommandAsync<RadDocking> RibbonOpenProjectCommand { get; set; }
    public RelayCommandAsync<RadDocking> RibbonCloseProjectCommand { get; set; }
    public RelayCommandAsync<RadDocking> RibbonAddWorkspaceCommand { get; set; }
    public RelayCommand<RadDocking> RibbonRemoveWorkspaceCommand { get; set; }
    
    public RelayCommand<object> RibbonAboutAppCommand { get; set; }

    #endregion
    
    #region Create all ShellViewModel commands
    
    private void CreateCommands()
    {
        ShellWindowLoadedCommandAsync = new RelayCommandAsync<RadDocking>(OnShellWindowLoadedAsync);
        ShellWindowClosingCommand = new RelayCommand<RadDocking>(OnShellWindowClosing);

        ShellWindowLocationChangedCommand = new RelayCommand<object>(OnShellWindowLocationChanged);
        ShellWindowSizeChangedCommand = new RelayCommand<object>(OnShellWindowSizeChanged);
        
        RibbonOpenProjectCommand = new RelayCommandAsync<RadDocking>(OpenProjectAsync);
        RibbonCloseProjectCommand = new RelayCommandAsync<RadDocking>(async (d) => await Task.CompletedTask);
        RibbonAddWorkspaceCommand = new RelayCommandAsync<RadDocking>(AddWorkspaceAsync);
        RibbonRemoveWorkspaceCommand = new RelayCommand<RadDocking>(RemoveWorkspace);
        
        RibbonAboutAppCommand = new RelayCommand<object>((o) =>
        {
            var aboutViewModel = new AboutAppViewModel();
            var aboutWin = new AboutAppWindow()
            {
                DataContext = aboutViewModel
            };
            var aboutDlg = new RadWindow()
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Header = "About QInsight",
                ResizeMode = ResizeMode.NoResize,
                Content = aboutWin,
                CanClose = false
            };
            aboutViewModel.SetParentWindow(aboutDlg);
            aboutDlg.ShowDialog();
        });
        
        PanelCloseCommandAsync = new RelayCommandAsync<StateChangeEventArgs>(ClosePanelAsync);
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
    private async Task OnShellWindowLoadedAsync(RadDocking docking)
    {
        try
        {
            shellRadDocking = docking;
            
            Application.Current.MainWindow.WindowState = ShellWindow.MainAppSettings.WinStyle.WinState;
            
            // Load drivers, protocols and controls
            pluginLoader = new PluginLoader(logger);
            driverPlugins = pluginLoader.GetPluginDetails<IDriverBase>("./Drivers");
            protocolPlugins = pluginLoader.GetPluginDetails<IProtocolBase>("./Protocols");
            
            // process app arguments
            var cmdArgs = Environment.GetCommandLineArgs();
            if (cmdArgs.Length == 2)
            {
                var projectFilePath = cmdArgs[1];
                if (File.Exists(projectFilePath) && Path.GetExtension(projectFilePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    await OpenProjectFileAsync(projectFilePath);
                }
            }
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
    
    private async Task ClosePanelAsync(StateChangeEventArgs arg)
    {
        // if you want to remove workspace view models when closing panes
        
        // var rd = arg.OriginalSource as RadDocking;
        // foreach (var pane in arg.Panes)
        // {
        //     if (rd?.DataContext is ShellWindowModel dataContext)
        //     {
        //         if (pane.DataContext is WorkspaceViewModel vm)
        //         {
        //             pane.RemoveFromParent();
        //             //await vm.CleanAsync();
        //             //ViewModels.Remove(vm);
        //         }
        //     }
        // }
        
        await Task.CompletedTask;
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
            await OpenProjectFileAsync(dlg.FileName);
        }
    }

    private async Task OpenProjectFileAsync(string filePath)
    {
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
   

    private async Task AddWorkspaceAsync(RadDocking docking)
    {
        var workspaceViewModel = new WorkspaceViewModel(eventAggregator);
        ViewModels.Add(workspaceViewModel);
        
        
        // Add new workspace to Solution Explorer
        eventAggregator.Publish(new AddWorkspaceEaMsg() { WorkspaceViewModel =  workspaceViewModel});

        await Task.CompletedTask;
    }
    
    private void RemoveWorkspace(RadDocking docking)
    {
        if (docking.ActivePane is not QRadDocumentPane pane)
        {
            eventAggregator.Publish(new LogMessage(LogLevel.Warn, "No active workspace to remove."));
            return;
        }
        
        RadWindow.Confirm(new DialogParameters()
        {
            Content = $"Do you want to remove workspace \"{pane.Header}\"?",
            Header = "Remove Workspace",
            Owner = Application.Current.MainWindow,
            DialogStartupLocation = WindowStartupLocation.CenterOwner,
            Closed = (_, arg) =>
            {
                if (arg.DialogResult != true) return;
                
                eventAggregator.Publish<RemoveWorkspaceFromMainMenuMsg>(new RemoveWorkspaceFromMainMenuMsg() { Name = pane.Name, Label = pane.Header.ToString()! });
                pane.RemoveFromParent();
            }
        });

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