using System.Windows;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.SyncfusionDocking;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Models.AppSettings;

namespace Qenex.QSuite.ViewModels;

public partial class ShellViewModel : PropertyChangedBaseWithValidation
{

    #region Private

    private readonly EventAggregator eventAggregator;
    private Logger loggger;
    private AppSettings appSettings;

    private SolutionExplorerViewModel solutionExplorerViewModel;
    private LogViewModel logViewModel;
    private PropertiesViewModel propertiesViewModel;
    private ControlsViewModel controlsViewModel;

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
    public int WindowHeight { get; set; } = 500;
    public int WindowWidth { get; set; } = 800;
    
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
    }
    private void ProcessAppSettings(string filename)
    {
        appSettings = AppSettings.LoadAppSettingsFromFile("QSuiteAppSettings.xml") ?? AppSettings.GetDefaultAppSettings();
        
        if (appSettings.WinStyle.Height != 0 || appSettings.WinStyle.Width != 0)
        {
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.Height = appSettings.WinStyle.Height;
                Application.Current.MainWindow.Width = appSettings.WinStyle.Width;
                Application.Current.MainWindow.WindowState = appSettings.WinStyle.WinState;
            }
        }
        
        SetSyncfusionTheme(appSettings.Design);
    }

    #endregion

}