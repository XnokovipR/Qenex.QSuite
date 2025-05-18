using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using Qenex.QLibs.QUI;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Models.AppSettings;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.Models.Project;
using Qenex.QSuite.Protocols.Protocol;
using MessageBox = System.Windows.MessageBox;
using ProjectData = Microsoft.VisualBasic.CompilerServices.ProjectData;
using WinApp = System.Windows.Application;
using Telerik.Windows.Controls;

namespace Qenex.QSuite.ViewModels;

public partial class ShellViewModel
{

    #region Windows command Properties
    
    public RelayCommand<object> OnWinLocationChangedCommand { get; set; }
    public RelayCommand<object> OnWinSizeChangedCommand { get; set; }
    public RelayCommand<RadDocking> OnLoadedCommand { get; set; }
    public RelayCommand<RadDocking> OnClosingCommand { get; set; }
	public RelayCommand<RadDocking> OnRadDockingLoadedCommand { get; set; }

	#endregion

	#region Ribbon Commands

	public RelayCommandAsync<RadDocking> OnOpenProjectCommand { get; set; }
    public RelayCommandAsync<RadDocking> OnCloseProjectCommand { get; set; }

    public RelayCommand<RadDocking> OnAddWorkspaceCommand { get; set; }
    public RelayCommand<RadDocking> OnRemoveWorkspaceCommand { get; set; }
    
    #endregion
    
    #region ViewModel  Commands
    
    public RelayCommand<RadDocking> OnWindowClosingCommand { get; set; }
    public RelayCommand<EventArgs> OnCloseButtonClickCommand { get; set; }
    
    public RelayCommand<MouseButtonEventArgs> OnMouseLeftButtonDownCommand { get; set; }
    
    #endregion

    #region Create all ShellViewModel commands
    
    private void CreateCommands()
    {
        //OnDiagramLoadedCommand = new RelayCommand<SfDiagram>(OnDiagramLoaded);
        OnWinLocationChangedCommand = new RelayCommand<object>(OnWindowLocationChanged);
        OnWinSizeChangedCommand = new RelayCommand<object>(OnWindowSizeChanged);
        OnLoadedCommand = new RelayCommand<RadDocking>(OnLoaded);
        OnClosingCommand = new RelayCommand<RadDocking>(OnClosing);

		OnRadDockingLoadedCommand = new RelayCommand<RadDocking>(OnRadDockingLoaded);

		OnOpenProjectCommand = new RelayCommandAsync<RadDocking>(OpenProjectAsync);
        OnCloseProjectCommand = new RelayCommandAsync<RadDocking>(CloseProjectAsync, CanCloseProject);
        OnAddWorkspaceCommand = new RelayCommand<RadDocking>(AddWorkspace, CanAddWorkspace);
        OnRemoveWorkspaceCommand = new RelayCommand<RadDocking>(RemoveWorkspace, CanRemoveWorkspace);
        
        OnWindowClosingCommand = new RelayCommand<RadDocking>(RemoveWindowOnClosingEvent);
        OnCloseButtonClickCommand = new RelayCommand<EventArgs>(RemoveWindowOnCloseButtonClick);
        OnMouseLeftButtonDownCommand = new RelayCommand<MouseButtonEventArgs>(OnMouseLeftButtonDown);
        
    }

    #endregion
    
    #region Menu command methods

    private async Task OpenProjectAsync(RadDocking RadDocking)
    {
        var openFileDialog = new System.Windows.Forms.OpenFileDialog
        {
            Filter = "QSuite project files (*.zip)|*.zip",
            Title = "Open QSuite Project",
            InitialDirectory = @$"{Directory.GetCurrentDirectory()}",
        };

        if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var filePath = openFileDialog.FileName;

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
                logger?.Log(LogLevel.Warn, $"Failed to load project file \"{Path.GetFileName(filePath)}\" ({e.Message}).");
                throw;
            }
            
        }
    }
    
    
    private bool CanCloseProject(object parameter)
    {
        return isProjectMade;
    }
    private async Task CloseProjectAsync(RadDocking RadDocking)
    {
        var result = MessageBox.Show("Are you sure you want to close the current project?", "Close Project", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            solutionExplorerViewModel.DisposeAll();

            ChangeIsProjectMade(false);
            await Task.CompletedTask;
        }
    }
    
    private bool CanAddWorkspace(object parameter)
    {
        return true;
        return isProjectMade;
    }
    private void AddWorkspace(RadDocking RadDocking)
    {
        //var rndNum = new Random().Next(1, 1000_000);
        //var newWorkspace = new WorkspaceViewModel(eventAggregator, RadDocking, $"{rndNum}");
        //DockManager.Add(newWorkspace);
    }
    
    private bool CanRemoveWorkspace(object parameter)
    {
        
        return isProjectMade;
    }
    private void RemoveWorkspace(RadDocking RadDocking)
    {
        // var workspace = solutionExplorerViewModel.GetSelectedWorkspace();
        // if (workspace != null)
        // {
        //     solutionExplorerViewModel.RemoveWorkspace(workspace);
        // }
    }

    #endregion

    #region ViewModel command methods

    private void RemoveWindowOnClosingEvent(RadDocking RadDocking)
    {

    }
    
    private void RemoveWindowOnCloseButtonClick(EventArgs eventArgs)
    {
        //var name = ((System.Windows.Controls.ContentControl)(eventArgs).TargetItem).Name;
        //var dockToRemove = DockManager.DockableViewModels.FirstOrDefault(i => i.Name == name);

        //if (dockToRemove == null) return;
        
        //DockManager.DockableViewModels.Remove(dockToRemove);
        //eventArgs.Cancel = true;

    }

    private void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        //if (!(e.Source is DocumentContainer documentContainer)) return;
        //if (!(documentContainer.ActiveDocument is ContentControl contentControl)) return;
        //if (!contentControl.Name.Contains("Workspace")) return;
        
        //var name = contentControl.Name;
        //var workspace = DockManager.DockableViewModels.FirstOrDefault(i => i.Name == name);
        //if (workspace == null) return;
        
        //logger.Log(LogLevel.Debug, $"You've clicked on Workspace: {workspace.Header} / {name}");
    }

    #endregion
    
    #region Window command methods

    private void OnWindowLocationChanged(object obj)
    {
        if (WinApp.Current.MainWindow == null) return;
        appSettings.WinStyle.Top = WinApp.Current.MainWindow.Top;
        appSettings.WinStyle.Left = WinApp.Current.MainWindow.Left;
    }
    private void OnWindowSizeChanged(object obj)
    {
        if (WinApp.Current.MainWindow == null) return;
        appSettings.WinStyle.Height = (int)WinApp.Current.MainWindow.Height;
        appSettings.WinStyle.Width = (int) WinApp.Current.MainWindow.Width;
        appSettings.WinStyle.WinState = WinApp.Current.MainWindow.WindowState;
    }

    // This method is called when the docking manager is loaded
    private void OnLoaded(RadDocking docking)
    {       
     
        if (!File.Exists("QenexEditLayout.xml")) return;
        
        try
        {
            //if (docking.DocContainer is DocumentContainer docContainer)
            //{
            //    docContainer.AddTabDocumentAtLast = true;
            //}
            
            //docking.LoadDockState("QenexEditLayout.xml");
            
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
    private void OnClosing(RadDocking docking)
    {
        try
        {
           // docking.SaveDockState("QenexEditLayout.xml");

            //appSettings.WinStyle.ScreenId = GetCurrentWindowScreen();
            AppSettings.SaveAppSettingsToFile("QSuiteAppSettings.xml", appSettings);
        }
        catch (Exception e)
        {
            eventAggregator.Publish(new LogMessage(LogLevel.Error, e.Message));
        }
    }

    private int GetCurrentWindowScreen()
    {
        var window = WinApp.Current.MainWindow;
        if (window == null) return -1;
        var windowHandle = new WindowInteropHelper(window).Handle;
        var screen = Screen.FromHandle(windowHandle);
        
        var screenBounds = screen.Bounds;
        var workingArea = screen.WorkingArea;
        bool isPrimary = screen.Primary; 

        var allScreens = Screen.AllScreens;
        int screenIndex = Array.IndexOf(allScreens, screen); 
        
        return screenIndex;
    }

	private void OnRadDockingLoaded(RadDocking RadDocking)
	{
		CreateViewModels();
	}

	#endregion

	#region Can-methods process

	private void ChangeIsProjectMade(bool isMade)
    {
        isProjectMade = isMade;
        OnCloseProjectCommand?.OnCanExecuteChanged();
        OnAddWorkspaceCommand?.OnCanExecuteChanged();
    }

    #endregion
}