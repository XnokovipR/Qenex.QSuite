using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using Qenex.QLibs.QUI;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Models.AppSettings;
using Syncfusion.Windows.Shared;
using Syncfusion.Windows.Tools.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.Models.Project;
using Qenex.QSuite.Protocols.Protocol;
using Syncfusion.UI.Xaml.Diagram;
using MessageBox = System.Windows.MessageBox;
using ProjectData = Microsoft.VisualBasic.CompilerServices.ProjectData;
using WinApp = System.Windows.Application;

namespace Qenex.QSuite.ViewModels;

public partial class ShellViewModel
{

    #region Windows command Properties
    
    public RelayCommand<object> OnWinLocationChangedCommand { get; set; }
    public RelayCommand<object> OnWinSizeChangedCommand { get; set; }
    public RelayCommand<DockingManager> OnLoadedCommand { get; set; }
    public RelayCommand<DockingManager> OnClosingCommand { get; set; }
    
    #endregion

    #region Ribbon Commands

    public RelayCommandAsync<DockingManager> OnOpenProjectCommand { get; set; }
    public RelayCommandAsync<DockingManager> OnCloseProjectCommand { get; set; }
    
    #endregion

    #region Create all ShellCiewModel commands
    
    private void CreateCommands()
    {
        //OnDiagramLoadedCommand = new RelayCommand<SfDiagram>(OnDiagramLoaded);
        OnWinLocationChangedCommand = new RelayCommand<object>(OnWindowLocationChanged);
        OnWinSizeChangedCommand = new RelayCommand<object>(OnWindowSizeChanged);
        OnLoadedCommand = new RelayCommand<DockingManager>(OnLoaded);
        OnClosingCommand = new RelayCommand<DockingManager>(OnClosing);

        OnOpenProjectCommand = new RelayCommandAsync<DockingManager>(OpenProjectAsync);
        OnCloseProjectCommand = new RelayCommandAsync<DockingManager>(CloseProjectAsync, CanCloseProject);
        
    }

    #endregion
    
    #region Menu command methods

    private async Task OpenProjectAsync(DockingManager dockingManager)
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
                realProjectData = RealProjectData.CreateRealProjectData(driverPlugins, protocolPlugins, projectData, logger);
                
                solutionExplorerViewModel.ReloadProjectData(realProjectData);
                
                isProjectLoaded = true;
                OnCloseProjectCommand.OnCanExecuteChanged();
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
        return isProjectLoaded;
    }
    private async Task CloseProjectAsync(DockingManager dockingManager)
    {
        var result = MessageBox.Show("Are you sure you want to close the current project?", "Close Project", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            solutionExplorerViewModel.DisposeAll();

            isProjectLoaded = false;
            OnCloseProjectCommand.OnCanExecuteChanged();
            await Task.CompletedTask;
        }
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
    private void OnLoaded(DockingManager docking)
    {
        if (!File.Exists("QenexEditLayout.xml")) return;
        
        try
        {
            docking.LoadDockState("QenexEditLayout.xml");
            
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
    private void OnClosing(DockingManager docking)
    {
        try
        {
            docking.SaveDockState("QenexEditLayout.xml");

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

    #endregion
}