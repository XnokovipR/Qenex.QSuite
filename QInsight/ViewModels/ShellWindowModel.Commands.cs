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
using Qenex.QSuite.Scripting.Script;
using Qenex.QSuite.Variables.QVariables;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Docking;

namespace Qenex.QInsight.ViewModels;

public partial class ShellWindowModel
{
    #region Fields

    // if true, only elements with the specified tag condition will be saved/cleaned in layout saving/cleaning process.
    // If false, elements with the specified tag condition will be excluded from layout saving/cleaning process.
    bool requireSerializationTag = false;
    
    #endregion
    
    #region Windows command Properties
   
    public RelayCommand<object> ShellWindowLocationChangedCommand { get; set; }
    public RelayCommand<object> ShellWindowSizeChangedCommand { get; set; }
    public RelayCommandAsync<RadDocking> ShellWindowLoadedCommandAsync { get; set; }
    public RelayCommand<RadDocking> ShellWindowClosingCommand { get; set; }
    
    public RelayCommandAsync<StateChangeEventArgs> PanelCloseCommandAsync { get; set; }
    
    public RelayCommand<LayoutSerializationCleaningEventArgs> DockingElementLayoutCleaningCommand { get; set; }
    public RelayCommand<LayoutSerializationSavingEventArgs> DockingElementLayoutSavingCommand { get; set; }
    
    #endregion

    #region Ribbon command Properties

    public RelayCommandAsync<RadDocking> RibbonOpenProjectCommand { get; set; }
    public RelayCommandAsync<RadDocking> RibbonSaveProjectCommand { get; set; }
    public RelayCommandAsync<RadDocking> RibbonSaveProjectAsCommand { get; set; }
    public RelayCommandAsync<RadDocking> RibbonCloseProjectCommand { get; set; }
    public RelayCommandAsync<RadDocking> RibbonAddWorkspaceCommand { get; set; }
    public RelayCommand<RadDocking> RibbonRemoveWorkspaceCommand { get; set; }
    
    public RelayCommandAsync<RadDocking> RibbonConnectCommand { get; set; }
    public RelayCommandAsync<RadDocking> RibbonDisconnectCommand { get; set; }
    
    public RelayCommand<RadDocking> RibbonScriptVariablesSettingsCommand { get; set; }
    public RelayCommand<RadDocking> RibbonScriptsSettingsCommand { get; set; }
    
    public RelayCommand<RadDocking> RibbonSaveLayoutCommand { get; set; }
    public RelayCommand<RadDocking> RibbonSaveWorkspaceLayoutCommand { get; set; }
    
    
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
        RibbonSaveProjectCommand = new RelayCommandAsync<RadDocking>(SaveProjectAsync);
        RibbonSaveProjectAsCommand = new RelayCommandAsync<RadDocking>(SaveProjectAsAsync);
        RibbonCloseProjectCommand = new RelayCommandAsync<RadDocking>(async (d) => await Task.CompletedTask);
        RibbonAddWorkspaceCommand = new RelayCommandAsync<RadDocking>(AddWorkspaceAsync);
        RibbonRemoveWorkspaceCommand = new RelayCommand<RadDocking>(RemoveWorkspace);

        RibbonConnectCommand = new RelayCommandAsync<RadDocking>(ConnectAsync);
        RibbonDisconnectCommand = new RelayCommandAsync<RadDocking>(DisconnectAsync);
        
        RibbonScriptVariablesSettingsCommand = new RelayCommand<RadDocking>(OpenScriptVariablesOptions);
        RibbonScriptsSettingsCommand = new RelayCommand<RadDocking>(RemoveScriptVariablesOptions);

        RibbonSaveLayoutCommand = new RelayCommand<RadDocking>(r =>
        {
            requireSerializationTag = false;
            SaveLayout(r);
        });
        
        RibbonSaveWorkspaceLayoutCommand = new RelayCommand<RadDocking>(r =>
        {
            requireSerializationTag = true;
            SaveLayout(r,"ws");
            requireSerializationTag = false;
        });
        
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
        DockingElementLayoutCleaningCommand =
            new RelayCommand<LayoutSerializationCleaningEventArgs>(DockingElementLayoutCleaning);
        DockingElementLayoutSavingCommand = new RelayCommand<LayoutSerializationSavingEventArgs>(DockingElementLayoutSaving);
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
            
            Application.Current.MainWindow!.WindowState = ShellWindow.MainAppSettings.WinStyle.WinState;
            
            // Load layout settings
            LoadSettingsFromFile(shellRadDocking, editModeSettingLayoutFile);
            
            
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
    
    private void DockingElementLayoutSaving(LayoutSerializationSavingEventArgs e)
    {
        CancelLayoutSavingByTagCondition(e, "WorkspaceViewModel", requireSerializationTag);
        // var tag = e.AffectedElementSerializationTag;
        // if (!tag.Contains("WorkspaceViewModel"))
        // {
        //     e.Cancel = true;
        // }
    }
    
    private void CancelLayoutSavingByTagCondition(LayoutSerializationSavingEventArgs eventArgs, string conditionString, bool reqSerializationTag)
    {
        var hasConditionString = eventArgs.AffectedElementSerializationTag.Contains(conditionString);
        if (hasConditionString != reqSerializationTag)
        {
            eventArgs.Cancel = true;
        }
    }
    
    private void DockingElementLayoutCleaning(LayoutSerializationCleaningEventArgs e)
    {
        CancelLayoutSCleaningByTagCondition(e, "WorkspaceViewModel", requireSerializationTag);
        // var tag = e.AffectedElementSerializationTag;
        // if (!tag.Contains("WorkspaceViewModel"))
        // {
        //     e.Cancel = true;
        // }
    }

    private void CancelLayoutSCleaningByTagCondition(LayoutSerializationCleaningEventArgs eventArgs, string conditionString, bool reqSerializationTag)
    {
        var hasConditionString = eventArgs.AffectedElementSerializationTag.Contains(conditionString);
        if (hasConditionString != reqSerializationTag)
        {
            eventArgs.Cancel = true;
        }
    }

	#endregion

	#region Ribbon command methods

    #region Project menu

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

            realProjectData = RealProjectData.CreateRealProjectData(driverPlugins, protocolPlugins, projectData, ShellWindow.MainAppSettings.ScriptEngine, logger);
            solutionExplorerViewModel.ReloadProjectData(realProjectData);
                
            currentProjectFilePath = Path.GetFullPath(filePath);
            ChangeIsProjectMade(true);
            logger.Log(LogLevel.Info, $"Project file \"{Path.GetFileName(filePath)}\" opened.");
                
        }
        catch (Exception e)
        {
            logger.Log(LogLevel.Error, e.Message);
        }        
    }
    
    private async Task SaveProjectAsync(object obj)
    {
        if (!isProjectMade)
        {
            logger.Log(LogLevel.Warn, "No project to save.");
            return;
        }

        if (string.IsNullOrWhiteSpace(currentProjectFilePath))
        {
            await SaveProjectAsAsync(obj);
            return;
        }

        await SaveProjectFileAsync(currentProjectFilePath);
    }
    
    private async Task SaveProjectAsAsync(object obj)
    {
        if (!isProjectMade)
        {
            logger.Log(LogLevel.Warn, "No project to save.");
            return;
        }
        
        var lastProjectPath = /*ShellWindow.MainAppSettings.LastProjectPath ??*/ Environment.CurrentDirectory;
        var dlg = new RadSaveFileDialog()
        {
            Owner = App.Current.MainWindow,
            Filter = "QInsight project files (*.zip)|*.zip",
            InitialDirectory = lastProjectPath
        };

        dlg.ShowDialog();

        
        if (dlg.DialogResult == true)
        {
            var saved = await SaveProjectFileAsync(dlg.FileName);
            if (saved)
            {
                currentProjectFilePath = Path.GetFullPath(dlg.FileName);
            }
        }
    }
    
    private async Task<bool> SaveProjectFileAsync(string filePath)
    {
        var tempFilePath = GetTempProjectFilePath(filePath);
        var hasProjectBackup = false;
        
        try
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
            
            if (File.Exists(filePath))
            {
                File.Move(filePath, tempFilePath);
                hasProjectBackup = true;
            }
            
            await ProjectZip.ZipProjectFileAsync(filePath, realProjectData, logger);
                
            if (hasProjectBackup)
            {
                File.Delete(tempFilePath);
            }
            
            logger.Log(LogLevel.Info, $"Project file \"{Path.GetFileName(filePath)}\" saved.");
            return true;
                
        }
        catch (Exception e)
        {
            RestoreProjectFile(filePath, tempFilePath, hasProjectBackup);
            logger.Log(LogLevel.Error, e.Message);
            return false;
        }        
    }

    private static string GetTempProjectFilePath(string filePath)
    {
        var directoryName = Path.GetDirectoryName(filePath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        var tempFileName = $"{fileNameWithoutExtension}_temp{extension}";

        return string.IsNullOrWhiteSpace(directoryName)
            ? tempFileName
            : Path.Combine(directoryName, tempFileName);
    }

    private void RestoreProjectFile(string filePath, string tempFilePath, bool hasProjectBackup)
    {
        if (!hasProjectBackup || !File.Exists(tempFilePath))
        {
            return;
        }

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.Move(tempFilePath, filePath);
        }
        catch (Exception restoreException)
        {
            logger.Log(LogLevel.Error, $"Project file \"{Path.GetFileName(filePath)}\" restore failed.", restoreException);
        }
    }

    #endregion
    
    #region Workspace menu

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
        if (docking.ActivePane is not QRadDocumentPane pane || !pane.Header.Equals("Workspace"))
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

    #region Script menu

    private void OpenScriptVariablesOptions(RadDocking docking)
    {
        var foundScriptVariablesSettingsViewModel = ViewModels.FirstOrDefault(vm => vm is IWorkspaceViewModel { Name: "ScriptVariablesSettingsViewModel" });
        if (foundScriptVariablesSettingsViewModel != null)
        {
            // switch to that
            if (foundScriptVariablesSettingsViewModel.IsHidden)
            {
                foundScriptVariablesSettingsViewModel.IsHidden =  false;
            }
        }
        else
        {
            var scriptVariablesSettingsViewModel = new ScriptVariablesSettingsViewModel(eventAggregator);
            ViewModels.Add(scriptVariablesSettingsViewModel);
        }
    }

    private void RemoveScriptVariablesOptions(RadDocking docking)
    {
        
    }

    #endregion

    #region Run menu

    private async Task ConnectAsync(object obj)
    {
        LoadSettingsFromFile(shellRadDocking, runtimeSettingLayoutFile);
        try
        {
            await realProjectData.Module.StartAsync();
            IsRuntimeStarted = true;
        }
        catch (Exception e)
        {
            IsRuntimeStarted = false;
            logger.Log(LogLevel.Error, e.Message);
        }
    }

    private async Task DisconnectAsync(object obj)
    {
        LoadSettingsFromFile(shellRadDocking, editModeSettingLayoutFile);
        try
        {
            await realProjectData.Module.StopAsync();
            IsRuntimeStarted = false;
        }
        catch (Exception e)
        {
            logger.Log(LogLevel.Error, e.Message);
        }
    }
    
    #endregion

    #region Options
    
    private void LoadSettingsFromFile(RadDocking radDocking, string settingsLayoutFile)
    {
        try
        {
            if (!File.Exists(settingsLayoutFile)) throw new FileNotFoundException($"Settings layout file \"{settingsLayoutFile}\" not found.");
            using var stream = new FileStream(settingsLayoutFile, FileMode.Open);
            //radDocking.Tag = "Settings";
            radDocking.LoadLayout(stream);
        }
        catch (Exception e)
        {
            logger.Log(LogLevel.Warn, $"Open settings file {settingsLayoutFile} failed.", e);
        }
    } 

    private void SaveLayout(RadDocking radDocking, string? filePrep = null)
    {
        var settingsLayoutFile = IsRuntimeStarted ? runtimeSettingLayoutFile : editModeSettingLayoutFile;
        if (filePrep != null)
        {
            settingsLayoutFile = $"{filePrep}_{settingsLayoutFile}";
        }
        
        try
        {
            using var stream = new FileStream(settingsLayoutFile, FileMode.Create);
            //radDocking.Tag = "Settings";
            radDocking.SaveLayout(stream);
            
            logger.Log(LogLevel.Info, $"Layout settings saved.");
        }
        catch (Exception e)
        {
            logger.Log(LogLevel.Error, $"Save settings file {settingsLayoutFile} failed.", e);
        }
    }

    #endregion

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
