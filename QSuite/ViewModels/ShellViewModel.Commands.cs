using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using Qenex.QLibs.QUI;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Models.AppSettings;
using Syncfusion.Windows.Shared;
using Syncfusion.Windows.Tools.Controls;
using System.Windows.Forms;
using Syncfusion.UI.Xaml.Diagram;
using WinApp = System.Windows.Application;

namespace Qenex.QSuite.ViewModels;

public partial class ShellViewModel
{
    #region Properties

    #region Commands
    
    public RelayCommand<object> OnWinLocationChangedCommand { get; set; }
    public RelayCommand<object> OnWinSizeChangedCommand { get; set; }
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
        //OnDiagramLoadedCommand = new RelayCommand<SfDiagram>(OnDiagramLoaded);
        OnWinLocationChangedCommand = new RelayCommand<object>(OnWindowLocationChanged);
        OnWinSizeChangedCommand = new RelayCommand<object>(OnWindowSizeChanged);
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
        var windowHandle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        var screen = System.Windows.Forms.Screen.FromHandle(windowHandle);
        
        var screenBounds = screen.Bounds;
        var workingArea = screen.WorkingArea;
        bool isPrimary = screen.Primary; 

        var allScreens = System.Windows.Forms.Screen.AllScreens;
        int screenIndex = Array.IndexOf(allScreens, screen); 
        
        return screenIndex;
    }
}