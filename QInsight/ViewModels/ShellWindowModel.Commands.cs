using System.IO;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.EventAggregatorMsgs;
using Qenex.QInsight.Helpers;
using Qenex.QInsight.Models.Project;
using Qenex.QInsight.ViewModels.ModelWrappers;
using Qenex.QInsight.Views;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.TelerikDocking;
using Qenex.QLibs.QUI.Wpf;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Scripting.ScriptingEngine;
using Qenex.QSuite.Variables.QVariables;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Docking;
using Telerik.Windows.Controls.FileDialogs;

namespace Qenex.QInsight.ViewModels;

public partial class ShellWindowModel
{
    #region Fields

    // if true, only elements with the specified tag condition will be saved/cleaned in layout saving/cleaning process.
    // If false, elements with the specified tag condition will be excluded from layout saving/cleaning process.
    bool requireSerializationTag = false;
    private bool isReplayMode;
    private IReplayDriver? activeReplayDriver;
    private List<(IDriverBase Driver, bool IsEnabled)>? replayDriverStates;
    private List<(IProtocolBase Protocol, bool IsEnabled)>? replayProtocolStates;
    private bool isReplayDataLogImported;
    private string? replayDataLogFilePath;
    private bool canUseHomeRibbon = true;
    private bool canConnectRuntime = true;
    private bool canDisconnectRuntime;
    private bool canImportDataLog = true;
    private bool canExportDataLog = true;
    private bool canReplay = true;
    private bool canStopReplay;
    private bool isUpdatingReplayPositionFromDriver;
    private bool isUpdatingReplayPositionTextFromPosition;
    private int replaySeekRequestVersion;
    private RadOpenFileDialog? openProjectDialog;
    private RadSaveFileDialog? saveProjectDialog;
    private RadOpenFileDialog? importDataLogDialog;
    private RadSaveFileDialog? exportDataLogDialog;
    private string? lastProjectDialogDirectory;
    private string? lastDataLogDialogDirectory;
    private string? lastGraphExportDialogDirectory;
    
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
    public RelayCommand<object> RibbonProjectConfigurationDriversCommand { get; set; }
    public RelayCommand<object> RibbonProjectConfigurationVariablesCommand { get; set; }
    public RelayCommand<object> RibbonProjectConfigurationPresentationsCommand { get; set; }
    public RelayCommand<object> RibbonProjectConfigurationEventsCommand { get; set; }
    public RelayCommand<object> RibbonProjectConfigurationScriptsCommand { get; set; }
    
    public RelayCommandAsync<RadDocking> RibbonConnectCommand { get; set; }
    public RelayCommandAsync<RadDocking> RibbonDisconnectCommand { get; set; }
    public RelayCommandAsync<RadDocking> RibbonImportDataLogCommand { get; set; }
    public RelayCommandAsync<RadDocking> RibbonExportDataLogCommand { get; set; }
    public RelayCommandAsync<RadDocking> RibbonReplayCommand { get; set; }
    public RelayCommandAsync<RadDocking> RibbonStopReplayCommand { get; set; }
    public RelayCommand<object> RibbonReplayPauseResumeCommand { get; set; }
    
    public RelayCommand<RadDocking> RibbonSaveLayoutCommand { get; set; }
    
    
    public RelayCommand<RadDocking> RibbonPythonInterpreterCommand { get; set; }
    public RelayCommand<object> RibbonAboutAppCommand { get; set; }

    #endregion
    
    #region Create all ShellViewModel commands
    
    private void CreateCommands()
    {
        ShellWindowLoadedCommandAsync = new RelayCommandAsync<RadDocking>(OnShellWindowLoadedAsync);
        ShellWindowClosingCommand = new RelayCommand<RadDocking>(OnShellWindowClosing);

        ShellWindowLocationChangedCommand = new RelayCommand<object>(OnShellWindowLocationChanged);
        ShellWindowSizeChangedCommand = new RelayCommand<object>(OnShellWindowSizeChanged);
        
        RibbonOpenProjectCommand = new RelayCommandAsync<RadDocking>(OpenProjectAsync, _ => canUseHomeRibbon);
        RibbonSaveProjectCommand = new RelayCommandAsync<RadDocking>(SaveProjectAsync, _ => CanUseProjectCommand());
        RibbonSaveProjectAsCommand = new RelayCommandAsync<RadDocking>(SaveProjectAsAsync, _ => CanUseProjectCommand());
        RibbonCloseProjectCommand = new RelayCommandAsync<RadDocking>(CloseProjectAsync, _ => CanUseProjectCommand());
        RibbonAddWorkspaceCommand = new RelayCommandAsync<RadDocking>(AddWorkspaceAsync, _ => CanUseProjectCommand());
        RibbonRemoveWorkspaceCommand = new RelayCommand<RadDocking>(RemoveWorkspace, _ => CanUseProjectCommand());
        RibbonProjectConfigurationDriversCommand = new RelayCommand<object>(
            _ => OpenProjectConfiguration(ProjectConfigurationSection.CommunicationDrivers),
            _ => CanUseProjectCommand());
        RibbonProjectConfigurationVariablesCommand = new RelayCommand<object>(
            _ => OpenProjectConfiguration(ProjectConfigurationSection.Variables),
            _ => CanUseProjectCommand());
        RibbonProjectConfigurationPresentationsCommand = new RelayCommand<object>(
            _ => OpenProjectConfiguration(ProjectConfigurationSection.Presentations),
            _ => CanUseProjectCommand());
        RibbonProjectConfigurationEventsCommand = new RelayCommand<object>(
            _ => OpenProjectConfiguration(ProjectConfigurationSection.Events),
            _ => CanUseProjectCommand());
        RibbonProjectConfigurationScriptsCommand = new RelayCommand<object>(
            _ => OpenProjectConfiguration(ProjectConfigurationSection.Scripts),
            _ => CanUseProjectCommand());

        RibbonConnectCommand = new RelayCommandAsync<RadDocking>(ConnectAsync, _ => CanConnectRuntime());
        RibbonDisconnectCommand = new RelayCommandAsync<RadDocking>(DisconnectAsync, _ => canDisconnectRuntime);
        RibbonImportDataLogCommand = new RelayCommandAsync<RadDocking>(ImportDataLogAsync, _ => CanImportDataLog());
        RibbonExportDataLogCommand = new RelayCommandAsync<RadDocking>(ExportDataLogAsync, _ => CanExportDataLog());
        RibbonReplayCommand = new RelayCommandAsync<RadDocking>(ReplayAsync, _ => CanStartReplay());
        RibbonStopReplayCommand = new RelayCommandAsync<RadDocking>(StopReplayAsync, _ => canStopReplay);
        RibbonReplayPauseResumeCommand = new RelayCommand<object>(_ => ToggleReplayPause(), _ => IsReplayControlEnabled);
        
        RibbonSaveLayoutCommand = new RelayCommand<RadDocking>(r =>
        {
            requireSerializationTag = false;
            SaveLayout(r);
        });

        RibbonPythonInterpreterCommand = new RelayCommand<RadDocking>(OpenPythonInterpreter);
        
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
        foreach (var pane in arg.Panes.ToList())
        {
            if (pane.DataContext is not PythonInterpreterViewModel pythonInterpreterViewModel)
            {
                continue;
            }

            await pythonInterpreterViewModel.CleanAsync();
            ViewModels.Remove(pythonInterpreterViewModel);
        }
    }
    
    private void DockingElementLayoutSaving(LayoutSerializationSavingEventArgs e)
    {
        CancelLayoutSavingByTagCondition(e, requireSerializationTag);
        // var tag = e.AffectedElementSerializationTag;
        // if (!tag.Contains("WorkspaceViewModel"))
        // {
        //     e.Cancel = true;
        // }
    }
    
    private void CancelLayoutSavingByTagCondition(LayoutSerializationSavingEventArgs eventArgs, bool reqSerializationTag)
    {
        var isWorkspaceLayoutElement = IsProjectWorkspaceLayoutElement(eventArgs.AffectedElementSerializationTag);
        if (isWorkspaceLayoutElement != reqSerializationTag)
        {
            eventArgs.Cancel = true;
        }
    }
    
    private void DockingElementLayoutCleaning(LayoutSerializationCleaningEventArgs e)
    {
        CancelLayoutSCleaningByTagCondition(e, requireSerializationTag);
        // var tag = e.AffectedElementSerializationTag;
        // if (!tag.Contains("WorkspaceViewModel"))
        // {
        //     e.Cancel = true;
        // }
    }

    private void CancelLayoutSCleaningByTagCondition(LayoutSerializationCleaningEventArgs eventArgs, bool reqSerializationTag)
    {
        var isWorkspaceLayoutElement = IsProjectWorkspaceLayoutElement(eventArgs.AffectedElementSerializationTag);
        if (isWorkspaceLayoutElement != reqSerializationTag)
        {
            eventArgs.Cancel = true;
        }
    }

    private static bool IsProjectWorkspaceLayoutElement(string serializationTag)
    {
        return serializationTag.Contains("WorkspaceViewModel", StringComparison.Ordinal);
    }

	#endregion

    #region Ribbon command methods

    private void OpenProjectConfiguration(ProjectConfigurationSection selectedSection)
    {
        var projectConfigurationViewModel = new ProjectConfigurationViewModel(
            eventAggregator,
            realProjectData.Module.Drivers,
            realProjectData.Module.Variables,
            realProjectData.Module.Presentations,
            realProjectData.Module.VarEvents,
            realProjectData.Module.Scripting.OnValueChangedScriptTriggers,
            realProjectData.Module.Scripting.Scripts);
        projectConfigurationViewModel.SelectSection(selectedSection);

        var projectConfigurationView = new ProjectConfigurationView()
        {
            DataContext = projectConfigurationViewModel
        };

        var projectConfigurationDialog = new RadWindow()
        {
            Owner = Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Header = "Project Configuration",
            Width = 1200,
            Height = 780,
            MinWidth = 600,
            MinHeight = 450,
            ResizeMode = ResizeMode.CanResize,
            Content = projectConfigurationView
        };

        projectConfigurationViewModel.SetParentWindow(projectConfigurationDialog);
        projectConfigurationDialog.ShowDialog();
    }

    #region Project menu

    private async Task OpenProjectAsync(object obj)
    {
        var initialDirectory = GetInitialDialogDirectory(lastProjectDialogDirectory, currentProjectFilePath);
        var dlg = openProjectDialog ??= CreateOpenFileDialog("QInsight project files (*.zip)|*.zip", initialDirectory);
        PrepareFileDialog(dlg, initialDirectory);

        dlg.ShowDialog();

        
        if (dlg.DialogResult == true)
        {
            lastProjectDialogDirectory = Path.GetDirectoryName(dlg.FileName);
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

            await CloseProjectWorkspacesAsync();
            isEditVariableEnabled = false;
            realProjectData = RealProjectData.CreateRealProjectData(driverPlugins, protocolPlugins, projectData, ShellWindow.MainAppSettings.ScriptEngine, logger);
            solutionExplorerViewModel.ReloadProjectData(realProjectData);
            LoadProjectWorkspaces(projectData.Workspaces);
            LoadProjectScriptDocuments(projectData.ScriptDocuments);
            LoadProjectPythonInterpreter(projectData.WorkspaceLayout);
            await Application.Current.Dispatcher.InvokeAsync(
                () => LoadWorkspaceLayoutFromData(projectData.WorkspaceLayout),
                DispatcherPriority.ApplicationIdle);
                
            currentProjectFilePath = Path.GetFullPath(filePath);
            SetProjectWindowTitle(currentProjectFilePath);
            ChangeIsProjectMade(true);
            ClearReplayDataLogImportState();
            NotifyRuntimeCommandsCanExecuteChanged();
            RibbonReplayCommand.OnCanExecuteChanged();
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
        
        var initialDirectory = GetInitialDialogDirectory(lastProjectDialogDirectory, currentProjectFilePath);
        var dlg = saveProjectDialog ??= CreateSaveFileDialog("QInsight project files (*.zip)|*.zip", initialDirectory);
        PrepareFileDialog(dlg, initialDirectory);

        dlg.ShowDialog();

        
        if (dlg.DialogResult == true)
        {
            lastProjectDialogDirectory = Path.GetDirectoryName(dlg.FileName);
            var saved = await SaveProjectFileAsync(dlg.FileName);
            if (saved)
            {
                currentProjectFilePath = Path.GetFullPath(dlg.FileName);
                SetProjectWindowTitle(currentProjectFilePath);
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
            
            var workspaces = CreateWorkspaceProjectData();
            var scriptDocuments = CreateScriptDocumentProjectData();
            using var workspaceLayoutStream = CreateWorkspaceLayoutStream(shellRadDocking);
            await ProjectZip.ZipProjectFileAsync(filePath, realProjectData, workspaces, scriptDocuments, workspaceLayoutStream, logger);
                
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

    private async Task CloseProjectAsync(object obj)
    {
        if (!isProjectMade)
        {
            logger.Log(LogLevel.Warn, "No project to close.");
            return;
        }

        if (!await ConfirmCloseProjectAsync())
        {
            return;
        }

        try
        {
            if (IsRuntimeStarted)
            {
                await realProjectData.Module.StopAsync();
                IsRuntimeStarted = false;
                SetRuntimeCommandStates(false);
            }
            
            await CloseProjectWorkspacesAsync();
            solutionExplorerViewModel.DisposeAll();
            
            realProjectData = null!;
            isEditVariableEnabled = false;
            currentProjectFilePath = null;
            SetProjectWindowTitle(currentProjectFilePath);
            ChangeIsProjectMade(false);
            ClearReplayDataLogImportState();
            NotifyRuntimeCommandsCanExecuteChanged();
            RibbonReplayCommand.OnCanExecuteChanged();
            
            ScriptLogsViewModel.ClearLog();
            LogsViewModel.ClearLog();
            SetDefaultPropertiesView();
        }
        catch (Exception e)
        {
            logger.Log(LogLevel.Error, e.Message);
        }
    }

    private Task<bool> ConfirmCloseProjectAsync()
    {
        var closeConfirmed = new TaskCompletionSource<bool>();
        RadWindow.Confirm(new DialogParameters()
        {
            Content = "Do you want to close current project?",
            Header = "Close Project",
            Owner = Application.Current.MainWindow,
            DialogStartupLocation = WindowStartupLocation.CenterOwner,
            Closed = (_, arg) => closeConfirmed.SetResult(arg.DialogResult == true)
        });

        return closeConfirmed.Task;
    }

    private async Task CloseProjectWorkspacesAsync()
    {
        var workspaceViewModels = ViewModels
            .Where(vm => vm is WorkspaceViewModel or ScriptViewModel or PythonInterpreterViewModel)
            .ToList();
        foreach (var workspaceViewModelBase in workspaceViewModels)
        {
            if (workspaceViewModelBase is IWorkspaceViewModel workspaceViewModel)
            {
                await workspaceViewModel.CleanAsync();
            }
            
            ViewModels.Remove(workspaceViewModelBase);
        }
    }

    #endregion
    
    #region Workspace menu

    private WorkspaceViewModel CreateWorkspaceViewModel()
    {
        return new WorkspaceViewModel(eventAggregator)
        {
            ConfigureGraphControlSaveDialog = FileDialogConfiguration.ConfigureFastFileDialog,
            GraphControlSaveDialogInitialDirectoryProvider = () =>
                GetInitialDialogDirectory(lastGraphExportDialogDirectory, currentProjectFilePath),
            GraphControlSaveDialogDirectoryChanged = directory => lastGraphExportDialogDirectory = directory
        };
    }

    private async Task AddWorkspaceAsync(RadDocking docking)
    {
        var workspaceViewModel = CreateWorkspaceViewModel();
        ViewModels.Add(workspaceViewModel);
        
        
        // Add new workspace to Solution Explorer
        eventAggregator.Publish(new AddWorkspaceEaMsg() { WorkspaceViewModel =  workspaceViewModel});

        await Task.CompletedTask;
    }

    private void LoadProjectWorkspaces(IEnumerable<WorkspaceProjectData> workspaces)
    {
        foreach (var workspace in workspaces)
        {
            var workspaceViewModel = CreateWorkspaceViewModel();
            workspaceViewModel.Name = workspace.Name;
            workspaceViewModel.WinTitle = workspace.WinTitle;
            workspaceViewModel.SetControlProjectData(workspace.Controls);
            workspaceViewModel.BindLoadedControlVariables(GetProjectProtocolVariables());

            ViewModels.Add(workspaceViewModel);
            solutionExplorerViewModel.AddWorkspace(workspaceViewModel);
        }
    }

    private void LoadProjectScriptDocuments(IEnumerable<ScriptDocumentProjectData> scriptDocuments)
    {
        foreach (var scriptDocument in scriptDocuments)
        {
            var script = realProjectData.Module.Scripting.Scripts.FirstOrDefault(script =>
                script.FileName.Equals(scriptDocument.FileName, StringComparison.OrdinalIgnoreCase));

            if (script == null)
            {
                logger.Log(LogLevel.Warn, $"Script document \"{scriptDocument.FileName}\" was not found in project.");
                continue;
            }

            var scriptWrapper = new ScriptWrapper(script, realProjectData.Module.Scripting);
            var scriptViewModel = new ScriptViewModel(eventAggregator, scriptWrapper)
            {
                Name = scriptDocument.Name
            };

            ViewModels.Add(scriptViewModel);
        }
    }
    
    private void RemoveWorkspace(RadDocking docking)
    {
        if (docking.ActivePane is not QRadDocumentPane pane || pane.DataContext is not WorkspaceViewModel workspaceViewModel)
        {
            eventAggregator.Publish(new LogMessage(LogLevel.Warn, "No active workspace to remove."));
            return;
        }
        
        RadWindow.Confirm(new DialogParameters()
        {
            Content = $"Do you want to remove workspace \"{workspaceViewModel.WinTitle}\"?",
            Header = "Remove Workspace",
            Owner = Application.Current.MainWindow,
            DialogStartupLocation = WindowStartupLocation.CenterOwner,
            Closed = (_, arg) =>
            {
                if (arg.DialogResult != true) return;
                
                workspaceViewModel.Clean();
                eventAggregator.Publish<RemoveWorkspaceFromMainMenuMsg>(new RemoveWorkspaceFromMainMenuMsg() { Name = workspaceViewModel.Name, Label = workspaceViewModel.WinTitle });
                ViewModels.Remove(workspaceViewModel);
                pane.RemoveFromParent();
            }
        });
    }
    
    #endregion

    private void OpenPythonInterpreter(RadDocking docking)
    {
        foreach (var pythonInterpreterViewModel in ViewModels.OfType<PythonInterpreterViewModel>().ToList())
        {
            if (FindDockingPaneForViewModel(docking, pythonInterpreterViewModel) != null)
            {
                pythonInterpreterViewModel.IsHidden = false;
                return;
            }

            ViewModels.Remove(pythonInterpreterViewModel);
        }

        ViewModels.Add(CreatePythonInterpreterViewModel());
    }

    private void LoadProjectPythonInterpreter(byte[]? workspaceLayoutData)
    {
        var layoutName = GetPythonInterpreterLayoutName(workspaceLayoutData);
        if (layoutName == null)
        {
            return;
        }

        if (ViewModels.OfType<PythonInterpreterViewModel>().Any())
        {
            return;
        }

        var pythonInterpreterViewModel = CreatePythonInterpreterViewModel();
        pythonInterpreterViewModel.Name = layoutName;
        ViewModels.Add(pythonInterpreterViewModel);
    }

    private PythonInterpreterViewModel CreatePythonInterpreterViewModel()
    {
        return new PythonInterpreterViewModel(
            eventAggregator,
            GetPythonInterpreterScriptingContextAsync);
    }

    private static string? GetPythonInterpreterLayoutName(byte[]? workspaceLayoutData)
    {
        if (workspaceLayoutData == null || workspaceLayoutData.Length == 0)
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(workspaceLayoutData);
            var document = XDocument.Load(stream);

            var pythonInterpreterPane = document
                .Descendants()
                .FirstOrDefault(element =>
                    element.Name.LocalName.Contains("Pane", StringComparison.Ordinal)
                    && IsPythonInterpreterLayoutPane(element));

            return pythonInterpreterPane?.Attribute("SerializationTag")?.Value;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsPythonInterpreterLayoutPane(XElement pane)
    {
        var serializationTag = pane.Attribute("SerializationTag")?.Value ?? string.Empty;
        if (serializationTag.Contains("PythonInterpreter", StringComparison.Ordinal))
        {
            return true;
        }

        var header = pane.Attribute("Header")?.Value ?? string.Empty;
        return pane.Name.LocalName.Equals("QRadDocumentPane", StringComparison.Ordinal)
               && (header.Equals("Python", StringComparison.Ordinal)
                   || header.Equals("Python Interpreter", StringComparison.Ordinal));
    }

    private static RadPane? FindDockingPaneForViewModel(RadDocking docking, object viewModel)
    {
        return docking
            .Panes
            .FirstOrDefault(pane => ReferenceEquals(pane.DataContext, viewModel));
    }

    private async Task<ScriptingContext?> GetPythonInterpreterScriptingContextAsync()
    {
        if (IsRuntimeStarted && realProjectData?.Module.Scripting.SharedScope != null)
        {
            return realProjectData.Module.Scripting;
        }

        if (pythonInterpreterStandaloneContext?.SharedScope != null)
        {
            return pythonInterpreterStandaloneContext;
        }

        pythonInterpreterStandaloneContext = new ScriptingContext(ShellWindow.MainAppSettings.ScriptEngine, logger);
        await pythonInterpreterStandaloneContext.InitializeSharedScopeAsync([]);
        return pythonInterpreterStandaloneContext;
    }

    #region Run menu

    private async Task ConnectAsync(object obj)
    {
        if (!isProjectMade || realProjectData?.Module == null)
        {
            logger.Log(LogLevel.Warn, "No project opened for runtime.");
            return;
        }

        LoadSettingsFromFile(shellRadDocking, runtimeSettingLayoutFile);
        try
        {
            ConfigureDataLoggerFileNames();
            realProjectData.Module.Scripting.IsReplayMode = false;
            await realProjectData.Module.StartAsync();
            IsRuntimeStarted = true;
            SetRuntimeCommandStates(true);
        }
        catch (Exception e)
        {
            IsRuntimeStarted = false;
            SetRuntimeCommandStates(false);
            logger.Log(LogLevel.Error, e.Message);
        }
    }

    private async Task DisconnectAsync(object obj)
    {
        if (isReplayMode)
        {
            await StopReplayAsync(obj);
            return;
        }

        LoadSettingsFromFile(shellRadDocking, editModeSettingLayoutFile);
        try
        {
            await realProjectData.Module.StopAsync();
            IsRuntimeStarted = false;
            SetRuntimeCommandStates(false);
        }
        catch (Exception e)
        {
            logger.Log(LogLevel.Error, e.Message);
        }
    }

    private void ConfigureDataLoggerFileNames()
    {
        var projectFileName = GetProjectDataLogFileName();
        foreach (var driver in realProjectData.Module.Drivers.OfType<IDataLogFileNameDriver>())
        {
            driver.DataLogFileName = projectFileName;
        }
    }

    private string GetProjectDataLogFileName()
    {
        var projectName = !string.IsNullOrWhiteSpace(currentProjectFilePath)
            ? Path.GetFileNameWithoutExtension(currentProjectFilePath)
            : realProjectData.Module.Specification.Name;

        projectName = SanitizeFileName(projectName);
        return string.IsNullOrWhiteSpace(projectName) ? "values" : projectName;
    }

    private static string SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        return new string(fileName
            .Trim()
            .Select(character => invalidCharacters.Contains(character) ? '_' : character)
            .ToArray());
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

    private void LoadWorkspaceLayoutFromData(byte[]? workspaceLayoutData)
    {
        if (workspaceLayoutData == null || workspaceLayoutData.Length == 0)
        {
            return;
        }

        var previousRequireSerializationTag = requireSerializationTag;
        try
        {
            requireSerializationTag = true;
            using var stream = new MemoryStream(workspaceLayoutData);
            shellRadDocking.LoadLayout(stream);
        }
        catch (Exception e)
        {
            logger.Log(LogLevel.Warn, "Open workspace layout from project file failed.", e);
        }
        finally
        {
            requireSerializationTag = previousRequireSerializationTag;
        }
    }

    private async Task ReplayAsync(object obj)
    {
        if (!isProjectMade)
        {
            logger.Log(LogLevel.Warn, "No project opened for replay.");
            return;
        }

        if (IsRuntimeStarted)
        {
            logger.Log(LogLevel.Warn, "Stop runtime before starting replay.");
            return;
        }

        if (!isReplayDataLogImported || string.IsNullOrWhiteSpace(replayDataLogFilePath) || !File.Exists(replayDataLogFilePath))
        {
            logger.Log(LogLevel.Warn, "Import a data log before starting replay.");
            return;
        }

        var replayDriver = FindReplayDriver();
        if (replayDriver == null)
        {
            logger.Log(LogLevel.Warn, "No FileDataReplayDriver found. Import a data log first.");
            return;
        }

        var replayProtocol = FindReplayProtocol(replayDriver);
        if (replayProtocol == null)
        {
            logger.Log(LogLevel.Warn, "No DataLogReplayProtocol found in replay driver.");
            return;
        }

        try
        {
            SaveReplayStates();
            ApplyReplayStates(replayDriver, replayProtocol);
            RefreshCommunicatedDriversProperties();
            SubscribeReplayCompleted(replayDriver);
            LoadSettingsFromFile(shellRadDocking, runtimeSettingLayoutFile);
            RebindWorkspaceControlVariables(replayProtocol.Variables);
            IsRuntimeStarted = true;
            isReplayMode = true;
            realProjectData.Module.Scripting.IsReplayMode = true;
            SetRuntimeCommandStates(true, true);
            SetReplayControlEnabled(true);
            await realProjectData.Module.StartAsync();

            if (!isReplayMode)
            {
                return;
            }

            solutionExplorerViewModel.ReloadProjectData(realProjectData, preserveWorkspaces: true);
            logger.Log(LogLevel.Info, "Replay started.");
        }
        catch (Exception e)
        {
            UnsubscribeReplayCompleted();
            RestoreReplayStates();
            LoadSettingsFromFile(shellRadDocking, editModeSettingLayoutFile);
            RebindWorkspaceControlVariables(GetProjectProtocolVariables());
            IsRuntimeStarted = false;
            isReplayMode = false;
            realProjectData.Module.Scripting.IsReplayMode = false;
            SetRuntimeCommandStates(false);
            SetReplayControlEnabled(false);
            logger.Log(LogLevel.Error, $"Replay start failed: {e.Message}");
        }
    }

    private async Task StopReplayAsync(object obj)
    {
        if (!isReplayMode)
        {
            logger.Log(LogLevel.Warn, "Replay is not running.");
            return;
        }

        var stopFailed = false;

        try
        {
            if (IsRuntimeStarted)
            {
                await realProjectData.Module.StopAsync();
            }
        }
        catch (Exception e)
        {
            stopFailed = true;
            logger.Log(LogLevel.Error, $"Replay runtime stop failed: {e.Message}");
        }
        finally
        {
            IsRuntimeStarted = false;
            isReplayMode = false;
            realProjectData.Module.Scripting.IsReplayMode = false;
            SetRuntimeCommandStates(false);
            UnsubscribeReplayCompleted();
            SetReplayControlEnabled(false);
            RestoreReplayStates();
            RefreshCommunicatedDriversProperties();
            LoadSettingsFromFile(shellRadDocking, editModeSettingLayoutFile);
            RebindWorkspaceControlVariables(GetProjectProtocolVariables());
            solutionExplorerViewModel.ReloadProjectData(realProjectData, preserveWorkspaces: true);

            if (!stopFailed)
            {
                logger.Log(LogLevel.Info, "Replay stopped.");
            }
        }
    }

    private async Task ImportDataLogAsync(object obj)
    {
        if (!isProjectMade)
        {
            logger.Log(LogLevel.Warn, "No project opened for data log import.");
            return;
        }

        if (IsRuntimeStarted)
        {
            logger.Log(LogLevel.Warn, "Stop runtime before importing a data log.");
            return;
        }

        var initialDirectory = GetInitialDialogDirectory(lastDataLogDialogDirectory, replayDataLogFilePath);
        var dlg = importDataLogDialog ??= CreateOpenFileDialog(
            "QSuite data log files (*.qilog)|*.qilog|Legacy MessagePack logs (*.msgpack)|*.msgpack|All files (*.*)|*.*",
            initialDirectory);
        PrepareFileDialog(dlg, initialDirectory);

        dlg.ShowDialog();
        if (dlg.DialogResult != true)
        {
            return;
        }

        try
        {
            lastDataLogDialogDirectory = Path.GetDirectoryName(dlg.FileName);
            var replayDriver = CreateFileDataReplayDriver(dlg.FileName);
            var existingReplayDriver = realProjectData.Module.Drivers.FirstOrDefault(driver =>
                driver.Specification.Name.Equals("FileDataReplayDriver", StringComparison.OrdinalIgnoreCase));
            if (existingReplayDriver != null)
            {
                existingReplayDriver.Dispose();
                realProjectData.Module.RemoveDriver(existingReplayDriver);
            }

            realProjectData.Module.AddDriver(replayDriver);
            StartReplayDataLoad(replayDriver);
            solutionExplorerViewModel.ReloadProjectData(realProjectData, preserveWorkspaces: true);
            SetReplayDataLogImportState(dlg.FileName);
            RibbonExportDataLogCommand.OnCanExecuteChanged();
            RibbonReplayCommand.OnCanExecuteChanged();
            logger.Log(LogLevel.Info, $"Data log \"{Path.GetFileName(dlg.FileName)}\" imported for replay.");
        }
        catch (Exception e)
        {
            logger.Log(LogLevel.Error, $"Data log import failed: {e.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task ExportDataLogAsync(object obj)
    {
        if (!CanExportDataLog())
        {
            logger.Log(LogLevel.Warn, "No data log imported for export.");
            return;
        }

        var initialDirectory = GetInitialDialogDirectory(lastDataLogDialogDirectory, replayDataLogFilePath);
        var dlg = exportDataLogDialog ??= CreateSaveFileDialog("CSV files (*.csv)|*.csv|All files (*.*)|*.*", initialDirectory);
        PrepareFileDialog(dlg, initialDirectory);
        dlg.FileName = GetDefaultDataLogCsvFileName();

        dlg.ShowDialog();
        if (dlg.DialogResult != true)
        {
            return;
        }

        try
        {
            var exportFilePath = EnsureFileExtension(dlg.FileName, ".csv");
            lastDataLogDialogDirectory = Path.GetDirectoryName(exportFilePath);
            var replayDriver = FindReplayDriver();
            if (replayDriver is not IDataLogCsvExportDriver exportDriver)
            {
                logger.Log(LogLevel.Warn, "Replay driver does not support CSV export.");
                return;
            }

            await exportDriver.ExportCsvAsync(exportFilePath);
            logger.Log(LogLevel.Info, $"Data log exported to \"{Path.GetFileName(exportFilePath)}\".");
        }
        catch (Exception e)
        {
            logger.Log(LogLevel.Error, $"Data log export failed: {e.Message}", e);
        }
    }

    private static RadOpenFileDialog CreateOpenFileDialog(string filter, string initialDirectory)
    {
        var dialog = new RadOpenFileDialog()
        {
            Owner = App.Current.MainWindow,
            Multiselect = false,
            Filter = filter,
            InitialDirectory = initialDirectory
        };

        FileDialogConfiguration.ConfigureFastFileDialog(dialog);
        return dialog;
    }

    private static RadSaveFileDialog CreateSaveFileDialog(string filter, string initialDirectory)
    {
        var dialog = new RadSaveFileDialog()
        {
            Owner = App.Current.MainWindow,
            Filter = filter,
            InitialDirectory = initialDirectory
        };

        FileDialogConfiguration.ConfigureFastFileDialog(dialog);
        return dialog;
    }

    private static void PrepareFileDialog(DialogWindowBase dialog, string initialDirectory)
    {
        FileDialogConfiguration.PrepareFileDialog(dialog, initialDirectory);
    }

    private static void ConfigureFastFileDialog(DialogWindowBase dialog)
    {
        FileDialogConfiguration.ConfigureFastFileDialog(dialog);
    }

    private static string GetInitialDialogDirectory(string? lastDirectory, string? currentFilePath)
    {
        return FileDialogConfiguration.GetInitialDialogDirectory(lastDirectory, currentFilePath);
    }

    private string GetDefaultDataLogCsvFileName()
    {
        if (!string.IsNullOrWhiteSpace(replayDataLogFilePath))
        {
            return $"{Path.GetFileNameWithoutExtension(replayDataLogFilePath)}.csv";
        }

        return $"{GetProjectDataLogFileName()}.csv";
    }

    private static string EnsureFileExtension(string filePath, string extension)
    {
        return Path.GetExtension(filePath).Equals(extension, StringComparison.OrdinalIgnoreCase)
            ? filePath
            : $"{filePath}{extension}";
    }

    private void StartReplayDataLoad(IDriverBase replayDriver)
    {
        if (replayDriver is not IReplayDriver replayDriverWithDataLoad)
        {
            return;
        }

        replayDriverWithDataLoad.StartLoadingData();
        logger.Log(LogLevel.Info, "Replay data loading started.");
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

    private List<WorkspaceProjectData> CreateWorkspaceProjectData()
    {
        return ViewModels
            .OfType<WorkspaceViewModel>()
            .Select(WorkspaceProjectData.FromWorkspace)
            .ToList();
    }

    private List<ScriptDocumentProjectData> CreateScriptDocumentProjectData()
    {
        return ViewModels
            .OfType<ScriptViewModel>()
            .Select(ScriptDocumentProjectData.FromScriptViewModel)
            .ToList();
    }

    private IEnumerable<IProtocolVariable> GetProjectProtocolVariables()
    {
        return realProjectData.Module.Drivers
            .SelectMany(driver => driver.Protocols)
            .SelectMany(protocol => protocol.Variables);
    }

    private IDriverBase CreateFileDataReplayDriver(string logFilePath)
    {
        var driverDetails = driverPlugins.FirstOrDefault(plugin =>
            plugin.Name.Equals("FileDataReplayDriver", StringComparison.OrdinalIgnoreCase));
        if (driverDetails == null)
        {
            throw new InvalidOperationException("FileDataReplayDriver plugin was not found.");
        }

        var protocolDetails = protocolPlugins.FirstOrDefault(plugin =>
            plugin.Name.Equals("DataLogReplayProtocol", StringComparison.OrdinalIgnoreCase));
        if (protocolDetails == null)
        {
            throw new InvalidOperationException("DataLogReplayProtocol plugin was not found.");
        }

        var driver = pluginLoader.LoadPlugin<IDriverBase>(driverDetails.PathName)
            ?? throw new InvalidOperationException("FileDataReplayDriver could not be loaded.");
        var protocol = pluginLoader.LoadPlugin<IProtocolBase>(protocolDetails.PathName)
            ?? throw new InvalidOperationException("DataLogReplayProtocol could not be loaded.");

        driver.Label = $"Replay: {Path.GetFileName(logFilePath)}";
        driver.IsEnabled = false;
        driver.RawSettings = $"file={logFilePath};mode=realtime;speed=1;loop=false";
        driver.SetConfiguration();

        protocol.IsEnabled = false;
        protocol.RawSettings = string.Empty;
        foreach (var variable in realProjectData.Module.Variables)
        {
            var protocolVariable = protocol.CreateProtocolVariable(variable, string.Empty, true);
            if (protocolVariable != null)
            {
                protocol.AddVariable(protocolVariable);
            }
        }

        protocol.SetConfiguration();
        driver.AddProtocol(protocol);

        return driver;
    }

    private IDriverBase? FindReplayDriver()
    {
        return realProjectData.Module.Drivers.FirstOrDefault(driver =>
            driver.Specification.Name.Equals("FileDataReplayDriver", StringComparison.OrdinalIgnoreCase));
    }

    private static IProtocolBase? FindReplayProtocol(IDriverBase replayDriver)
    {
        return replayDriver.Protocols.FirstOrDefault(protocol =>
            protocol.Specification.Name.Equals("DataLogReplayProtocol", StringComparison.OrdinalIgnoreCase));
    }

    private void SaveReplayStates()
    {
        replayDriverStates = realProjectData.Module.Drivers
            .Select(driver => (driver, driver.IsEnabled))
            .ToList();
        replayProtocolStates = realProjectData.Module.Drivers
            .SelectMany(driver => driver.Protocols)
            .Select(protocol => (protocol, protocol.IsEnabled))
            .ToList();
    }

    private void ApplyReplayStates(IDriverBase replayDriver, IProtocolBase replayProtocol)
    {
        foreach (var driver in realProjectData.Module.Drivers)
        {
            driver.IsEnabled = ReferenceEquals(driver, replayDriver);
        }

        foreach (var protocol in realProjectData.Module.Drivers.SelectMany(driver => driver.Protocols))
        {
            protocol.IsEnabled = ReferenceEquals(protocol, replayProtocol);
        }

    }

    private void RestoreReplayStates()
    {
        if (replayDriverStates != null)
        {
            foreach (var (driver, isEnabled) in replayDriverStates)
            {
                driver.IsEnabled = isEnabled;
            }
        }

        if (replayProtocolStates != null)
        {
            foreach (var (protocol, isEnabled) in replayProtocolStates)
            {
                protocol.IsEnabled = isEnabled;
            }
        }

        replayDriverStates = null;
        replayProtocolStates = null;
    }

    private void RefreshCommunicatedDriversProperties()
    {
        if (propertiesViewModel.SelectedViewModel is CommunicatedDriversPropertiesViewModel driversPropertiesViewModel)
        {
            driversPropertiesViewModel.RefreshDrivers();
        }

        if (propertiesViewModel.SelectedViewModel is DriverPropertiesViewModel driverPropertiesViewModel)
        {
            driverPropertiesViewModel.RefreshDriver();
        }

        if (propertiesViewModel.SelectedViewModel is CommunicatedProtocolsPropertiesViewModel protocolsPropertiesViewModel)
        {
            protocolsPropertiesViewModel.RefreshProtocols();
        }
    }

    private void RefreshProjectConfigurationAppliedProperties()
    {
        RefreshCommunicatedDriversProperties();
        RefreshScriptProperties();
        RebindWorkspaceControlVariables(GetProjectProtocolVariables());
        solutionExplorerViewModel.ReloadProjectData(realProjectData, preserveWorkspaces: true);

        switch (propertiesViewModel.SelectedViewModel)
        {
            case VariablePropertiesViewModel variablePropertiesViewModel:
                variablePropertiesViewModel.RefreshProperties();
                break;
            case ReadOnlyVariablePropertiesViewModel readOnlyVariablePropertiesViewModel:
                readOnlyVariablePropertiesViewModel.RefreshProperties();
                break;
        }
    }

    private void RefreshScriptProperties()
    {
        if (propertiesViewModel.SelectedViewModel is ScriptPropertiesViewModel scriptPropertiesViewModel)
        {
            scriptPropertiesViewModel.RefreshScript();
        }

        foreach (var scriptViewModel in ViewModels.OfType<ScriptViewModel>())
        {
            scriptViewModel.ScriptWrapper.RefreshProperties();
        }
    }

    private void RebindWorkspaceControlVariables(IEnumerable<IProtocolVariable> protocolVariables)
    {
        var protocolVariablesList = protocolVariables.ToList();
        foreach (var workspaceViewModel in ViewModels.OfType<WorkspaceViewModel>())
        {
            workspaceViewModel.BindLoadedControlVariables(protocolVariablesList);
        }
    }

    private void SubscribeReplayCompleted(IDriverBase replayDriver)
    {
        if (replayDriver is not IReplayDriver replayDriverWithEvents)
        {
            logger.Log(LogLevel.Warn,
                $"Replay driver \"{replayDriver.Specification.Name}\" does not implement IReplayDriver — natural end-of-replay will not be detected.");
            return;
        }

        activeReplayDriver = replayDriverWithEvents;
        activeReplayDriver.ReplayCompleted += OnReplayCompleted;
        activeReplayDriver.ReplayProgressChanged += OnReplayProgressChanged;
        UpdateReplayProgress(
            activeReplayDriver.CurrentTime,
            activeReplayDriver.Duration,
            activeReplayDriver.IsPaused,
            activeReplayDriver.IsDataLoaded);
    }

    private void UnsubscribeReplayCompleted()
    {
        if (activeReplayDriver == null)
        {
            return;
        }

        activeReplayDriver.ReplayCompleted -= OnReplayCompleted;
        activeReplayDriver.ReplayProgressChanged -= OnReplayProgressChanged;
        activeReplayDriver = null;
    }

    private async void OnReplayCompleted(object? sender, EventArgs e)
    {
        try
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                logger.Log(LogLevel.Info, "Replay completed — stopping replay session.");
                if (isReplayMode)
                {
                    await StopReplayAsync(shellRadDocking);
                }
            }).Task.Unwrap();
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Error, $"Replay completion handling failed: {ex.Message}", ex);
        }
    }

    private async void OnReplayProgressChanged(object? sender, ReplayProgressChangedEventArgs e)
    {
        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
                UpdateReplayProgress(e.CurrentTime, e.Duration, e.IsPaused, e.IsDataLoaded));
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Error, $"Replay progress update failed: {ex.Message}", ex);
        }
    }

    private void ToggleReplayPause()
    {
        if (activeReplayDriver == null)
        {
            return;
        }

        if (activeReplayDriver.IsPaused)
        {
            activeReplayDriver.Resume();
        }
        else
        {
            activeReplayDriver.Pause();
        }
    }

    private async Task SeekReplayPositionAsync(double seconds, int requestVersion)
    {
        await Task.Delay(150);
        if (requestVersion != replaySeekRequestVersion
            || activeReplayDriver == null
            || !isReplayMode
            || !activeReplayDriver.IsDataLoaded
            || !activeReplayDriver.IsPaused)
        {
            return;
        }

        try
        {
            await activeReplayDriver.SeekAsync(TimeSpan.FromSeconds(seconds));
        }
        catch (Exception e)
        {
            logger.Log(LogLevel.Error, $"Replay seek failed: {e.Message}", e);
        }
    }

    private void SetReplayControlEnabled(bool isEnabled)
    {
        IsReplayControlEnabled = isEnabled;
        if (!isEnabled)
        {
            UpdateReplayProgress(TimeSpan.Zero, TimeSpan.Zero, false, false);
        }

        RibbonReplayPauseResumeCommand.OnCanExecuteChanged();
    }

    private void UpdateReplayProgress(TimeSpan currentTime, TimeSpan duration, bool isPaused, bool isDataLoaded)
    {
        isUpdatingReplayPositionFromDriver = true;
        try
        {
            ReplayDurationSeconds = duration.TotalSeconds;
            ReplayPositionSeconds = currentTime.TotalSeconds;
            ReplayPauseResumeText = isPaused ? "Resume" : "Pause";
            IsReplaySeekEnabled = IsReplayControlEnabled && isDataLoaded && isPaused;
        }
        finally
        {
            isUpdatingReplayPositionFromDriver = false;
        }
    }

    private static string FormatReplayTime(TimeSpan time)
    {
        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss\.fff")
            : time.ToString(@"mm\:ss\.fff");
    }

    private void SetReplayPositionText(double seconds)
    {
        isUpdatingReplayPositionTextFromPosition = true;
        try
        {
            ReplayPositionText = FormatReplayTime(TimeSpan.FromSeconds(seconds));
        }
        finally
        {
            isUpdatingReplayPositionTextFromPosition = false;
        }
    }

    private static bool TryParseReplayTime(string value, out TimeSpan time)
    {
        var replayTimeText = value.Trim();
        var formats = new[]
        {
            @"h\:mm\:ss\.fff",
            @"h\:mm\:ss\.ff",
            @"h\:mm\:ss\.f",
            @"hh\:mm\:ss\.fff",
            @"hh\:mm\:ss\.ff",
            @"hh\:mm\:ss\.f",
            @"h\:mm\:ss",
            @"hh\:mm\:ss",
            @"m\:ss\.fff",
            @"m\:ss\.ff",
            @"m\:ss\.f",
            @"mm\:ss\.fff",
            @"mm\:ss\.ff",
            @"mm\:ss\.f",
            @"m\:ss",
            @"mm\:ss"
        };

        if (TimeSpan.TryParseExact(replayTimeText, formats, CultureInfo.InvariantCulture, out time))
        {
            return true;
        }

        if (double.TryParse(replayTimeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            time = TimeSpan.FromSeconds(seconds);
            return true;
        }

        return false;
    }

    private MemoryStream CreateWorkspaceLayoutStream(RadDocking radDocking)
    {
        var stream = new MemoryStream();
        var previousRequireSerializationTag = requireSerializationTag;

        try
        {
            requireSerializationTag = true;
            radDocking.SaveLayout(stream);
            stream.Position = 0;
            return stream;
        }
        finally
        {
            requireSerializationTag = previousRequireSerializationTag;
        }
    }

    #endregion

	#endregion
    
    #region Can-methods process

    private void ChangeIsProjectMade(bool isMade)
    {
        isProjectMade = isMade;
        NotifyProjectCommandsCanExecuteChanged();
        NotifyRuntimeCommandsCanExecuteChanged();
    }

    private void SetRuntimeCommandStates(bool runtimeStarted, bool replayStarted = false)
    {
        canUseHomeRibbon = !runtimeStarted;
        canConnectRuntime = !runtimeStarted;
        canDisconnectRuntime = runtimeStarted && !replayStarted;
        canImportDataLog = !runtimeStarted;
        canExportDataLog = !runtimeStarted;
        canReplay = !runtimeStarted;
        canStopReplay = replayStarted;

        RibbonOpenProjectCommand.OnCanExecuteChanged();
        RibbonSaveProjectCommand.OnCanExecuteChanged();
        RibbonSaveProjectAsCommand.OnCanExecuteChanged();
        RibbonCloseProjectCommand.OnCanExecuteChanged();
        RibbonAddWorkspaceCommand.OnCanExecuteChanged();
        RibbonRemoveWorkspaceCommand.OnCanExecuteChanged();
        RibbonProjectConfigurationDriversCommand.OnCanExecuteChanged();
        RibbonProjectConfigurationVariablesCommand.OnCanExecuteChanged();
        RibbonProjectConfigurationPresentationsCommand.OnCanExecuteChanged();
        RibbonProjectConfigurationEventsCommand.OnCanExecuteChanged();
        RibbonProjectConfigurationScriptsCommand.OnCanExecuteChanged();
        RibbonConnectCommand.OnCanExecuteChanged();
        RibbonDisconnectCommand.OnCanExecuteChanged();
        RibbonImportDataLogCommand.OnCanExecuteChanged();
        RibbonExportDataLogCommand.OnCanExecuteChanged();
        RibbonReplayCommand.OnCanExecuteChanged();
        RibbonStopReplayCommand.OnCanExecuteChanged();
        RibbonReplayPauseResumeCommand.OnCanExecuteChanged();
    }

    private void NotifyRuntimeCommandsCanExecuteChanged()
    {
        RibbonConnectCommand.OnCanExecuteChanged();
        RibbonImportDataLogCommand.OnCanExecuteChanged();
        RibbonExportDataLogCommand.OnCanExecuteChanged();
        RibbonReplayCommand.OnCanExecuteChanged();
    }

    private void NotifyProjectCommandsCanExecuteChanged()
    {
        RibbonSaveProjectCommand.OnCanExecuteChanged();
        RibbonSaveProjectAsCommand.OnCanExecuteChanged();
        RibbonCloseProjectCommand.OnCanExecuteChanged();
        RibbonAddWorkspaceCommand.OnCanExecuteChanged();
        RibbonRemoveWorkspaceCommand.OnCanExecuteChanged();
        RibbonProjectConfigurationDriversCommand.OnCanExecuteChanged();
        RibbonProjectConfigurationVariablesCommand.OnCanExecuteChanged();
        RibbonProjectConfigurationPresentationsCommand.OnCanExecuteChanged();
        RibbonProjectConfigurationEventsCommand.OnCanExecuteChanged();
        RibbonProjectConfigurationScriptsCommand.OnCanExecuteChanged();
    }

    private bool CanUseProjectCommand()
    {
        return canUseHomeRibbon && isProjectMade && realProjectData?.Module != null;
    }

    private bool CanConnectRuntime()
    {
        return canConnectRuntime && isProjectMade && realProjectData?.Module != null;
    }

    private bool CanImportDataLog()
    {
        return canImportDataLog && isProjectMade && realProjectData?.Module != null;
    }

    private bool CanExportDataLog()
    {
        return canExportDataLog
               && isProjectMade
               && isReplayDataLogImported
               && !string.IsNullOrWhiteSpace(replayDataLogFilePath)
               && File.Exists(replayDataLogFilePath)
               && FindReplayDriver() is IDataLogCsvExportDriver;
    }

    private bool CanStartReplay()
    {
        if (!canReplay ||
            !isProjectMade ||
            !isReplayDataLogImported ||
            string.IsNullOrWhiteSpace(replayDataLogFilePath) ||
            !File.Exists(replayDataLogFilePath) ||
            realProjectData?.Module == null)
        {
            return false;
        }

        var replayDriver = FindReplayDriver();
        return replayDriver != null && FindReplayProtocol(replayDriver) != null;
    }

    private void SetReplayDataLogImportState(string filePath)
    {
        replayDataLogFilePath = Path.GetFullPath(filePath);
        isReplayDataLogImported = true;
        RibbonExportDataLogCommand.OnCanExecuteChanged();
    }

    private void ClearReplayDataLogImportState()
    {
        replayDataLogFilePath = null;
        isReplayDataLogImported = false;
        RibbonExportDataLogCommand.OnCanExecuteChanged();
    }

    #endregion

}
