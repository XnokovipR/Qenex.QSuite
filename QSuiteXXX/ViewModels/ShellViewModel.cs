using System.Windows;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Models.AppSettings;
using Qenex.QSuite.Models.Project;
using System.Collections.ObjectModel;
using Telerik.Windows.Controls;

namespace Qenex.QSuite.ViewModels;

public partial class ShellViewModel : PropertyChangedBaseWithValidation
{
    #region Private

    private readonly EventAggregator eventAggregator;
    private readonly Logger logger;
    private AppSettings appSettings;
    
    // Plugins
    private PluginLoader pluginLoader;
    private List<PluginDetails> driverPlugins = null!;
    private List<PluginDetails> protocolPlugins = null!;
    
    // Project
    private RealProjectData realProjectData;
    private bool isProjectMade;
    
    // ViewModels
    private SolutionExplorerViewModel solutionExplorerViewModel;
    private LogViewModel logViewModel;
    private PropertiesViewModel propertiesViewModel;
    private ControlsViewModel controlsViewModel;

    #endregion

    #region Constructors

    public ShellViewModel()
    {
		ChangeIsProjectMade(false);
        ProcessAppSettings("QSuiteAppSettings.xml");
        
        eventAggregator = new EventAggregator();
        logger = new Logger(LogLevel.Trace);
        
        pluginLoader = new PluginLoader();

        ViewModels = new ObservableCollection<ViewModelBase>();
		CreateCommands();
    }

    #endregion

    #region Properties
    public ObservableCollection<ViewModelBase> ViewModels { get; set; }
    
    public int WindowHeight { get; set; } = 900;
    public int WindowWidth { get; set; } = 1600;
    public WindowState WinState { get; set; } = WindowState.Normal;
    
    
    #endregion

    #region Private
    
    private void CreateViewModels()
    {
        solutionExplorerViewModel = new SolutionExplorerViewModel(eventAggregator);
        ViewModels.Add(solutionExplorerViewModel);

        controlsViewModel = new ControlsViewModel(eventAggregator);
        ViewModels.Add(controlsViewModel);

        logViewModel = new LogViewModel(eventAggregator);
        logger.RegisterSubscriber(logViewModel);
        ViewModels.Add(logViewModel);

        propertiesViewModel = new PropertiesViewModel(eventAggregator);
        ViewModels.Add(propertiesViewModel);
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