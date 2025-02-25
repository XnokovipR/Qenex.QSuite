using System.Windows;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.SyncfusionDocking;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Models.AppSettings;
using Syncfusion.UI.Xaml.Diagram;
using WindowStyle = System.Windows.WindowStyle;

namespace Qenex.QSuite.ViewModels;

public partial class ShellViewModel : PropertyChangedBaseWithValidation
{
    #region Private

    private readonly EventAggregator eventAggregator;
    private Logger loggger;
    private AppSettings appSettings;

    private SfDiagram diagram;
    
    private SolutionExplorerViewModel solutionExplorerViewModel;
    private LogViewModel logViewModel;
    private PropertiesViewModel propertiesViewModel;
    private ControlsViewModel controlsViewModel;
    private WorkspaceViewModel workspaceViewModel;
    

    #endregion

    #region Constructors

    public ShellViewModel()
    {
        ProcessAppSettings("QSuiteAppSettings.xml");
        
        eventAggregator = new EventAggregator();
        loggger = new Logger(LogLevel.Trace);

        CreateViewModels();
        CreateCommands();
        
        loggger.Log(LogLevel.Error, "Testing error message");
        eventAggregator.Publish(new LogMessage(LogLevel.Debug, "Testing debug message"));
    }

    #endregion

    #region Properties
    public SyncfusionDockingManager DockingManager { get; set; }
    
    public int WindowHeight { get; set; } = 900;
    public int WindowWidth { get; set; } = 1600;
    public WindowState WinState { get; set; } = WindowState.Normal;
    
    
    #endregion

    #region Private
    
    private void CreateViewModels()
    {
        DockingManager = new SyncfusionDockingManager();

        solutionExplorerViewModel = new SolutionExplorerViewModel(eventAggregator);
        DockingManager.Add(solutionExplorerViewModel);
        
        controlsViewModel = new ControlsViewModel(eventAggregator);
        DockingManager.Add(controlsViewModel);

        logViewModel = new LogViewModel(eventAggregator);
        loggger.RegisterSubscriber(logViewModel);
        DockingManager.Add(logViewModel);
        
        propertiesViewModel = new PropertiesViewModel(eventAggregator);
        DockingManager.Add(propertiesViewModel);
        
        workspaceViewModel = new WorkspaceViewModel(eventAggregator);
        DockingManager.Add(workspaceViewModel);
    }
    private void ProcessAppSettings(string filename)
    {
        appSettings = AppSettings.LoadAppSettingsFromFile("QSuiteAppSettings.xml") ?? AppSettings.GetDefaultAppSettings();
        if (Application.Current.MainWindow != null)
        {
            var win = Application.Current.MainWindow;
            win.Height = appSettings.WinStyle.Height;
            win.Width = appSettings.WinStyle.Width;
            win.WindowState = appSettings.WinStyle.WinState;
            win.Top = appSettings.WinStyle.Top;
            win.Left = appSettings.WinStyle.Left;
            
            if (!appSettings.IsAppSettingRead)
            {
                win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                appSettings.WinStyle.ScreenId = 0;
                appSettings.WinStyle.Height = win.Height;
                appSettings.WinStyle.Width = win.Width;
                appSettings.WinStyle.WinState = win.WindowState;
                appSettings.WinStyle.Top = win.Top;
                appSettings.WinStyle.Left = win.Left;
                appSettings.IsAppSettingRead = true;
            }
            
            OpenWindowOnDefinedScreen(win, appSettings);
        }
        
        
        SetSyncfusionTheme(appSettings.Design);
    }

    private void OpenWindowOnDefinedScreen(Window win, AppSettings appSettings)
    {
        var targetScreen = System.Windows.Forms.Screen.AllScreens[appSettings.WinStyle.ScreenId];
        win.Left = targetScreen.Bounds.Left + appSettings.WinStyle.Left;
        win.Top = targetScreen.Bounds.Top + appSettings.WinStyle.Top;
    }

    #endregion

}