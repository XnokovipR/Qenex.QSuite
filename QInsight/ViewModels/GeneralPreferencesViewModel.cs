using System.IO;
using System.Windows;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.Helpers;
using Qenex.QLibs.QUI;
using Qenex.QSuite.LogSystems.LogSystem;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.FileDialogs;

namespace Qenex.QInsight.ViewModels;

public class GeneralPreferencesViewModel : PropertyChangedBaseWithValidation
{
    private readonly AppSettings appSettings;
    private readonly Logger logger;
    private readonly Action? settingsApplied;
    private RadWindow? parentWindow;

    public GeneralPreferencesViewModel(AppSettings appSettings, Logger logger, Action? settingsApplied = null)
    {
        this.appSettings = appSettings;
        this.logger = logger;
        this.settingsApplied = settingsApplied;

        UsePythonScripts = appSettings.ScriptEngine.UsePythonScripts;
        PythonDllPath = appSettings.ScriptEngine.PythonDllPath;
        ShowDebugLogMessages = appSettings.ShowDebugLogMessages;

        ApplyCommand = new RelayCommand<object>(_ => ApplySettings());
        OkCommand = new RelayCommand<object>(_ =>
        {
            if (ApplySettings())
            {
                parentWindow?.Close();
            }
        });
        CancelCommand = new RelayCommand<object>(_ => parentWindow?.Close());
        BrowsePythonDllCommand = new RelayCommand<object>(_ => BrowsePythonDll());
    }

    // Python is optional. When enabled, the DLL path is mandatory and must exist (validated
    // on Apply); when disabled, the path is kept but ignored by the application.
    public bool UsePythonScripts
    {
        get;
        set { field = value; OnPropertyChanged(); }
    }

    public string PythonDllPath
    {
        get;
        set { field = value; OnPropertyChanged(); }
    }

    public bool ShowDebugLogMessages
    {
        get;
        set { field = value; OnPropertyChanged(); }
    }

    public string ErrorMessage
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    } = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public RelayCommand<object> ApplyCommand { get; }
    public RelayCommand<object> OkCommand { get; }
    public RelayCommand<object> CancelCommand { get; }
    public RelayCommand<object> BrowsePythonDllCommand { get; }

    public void SetParentWindow(RadWindow window)
    {
        parentWindow = window;
    }

    private bool ApplySettings()
    {
        var pythonDllPath = PythonDllPath.Trim();
        if (UsePythonScripts)
        {
            if (string.IsNullOrWhiteSpace(pythonDllPath))
            {
                ErrorMessage = "Python Dll Path must be set when 'Use Python scripts' is enabled.";
                return false;
            }

            if (!File.Exists(pythonDllPath))
            {
                ErrorMessage = $"The Python DLL '{pythonDllPath}' does not exist.";
                return false;
            }
        }

        try
        {
            appSettings.ScriptEngine.UsePythonScripts = UsePythonScripts;
            appSettings.ScriptEngine.PythonDllPath = pythonDllPath;
            appSettings.ShowDebugLogMessages = ShowDebugLogMessages;

            AppSettings.SaveAppSettingsToFile(AppDataPaths.AppSettingsFile, appSettings);
            ErrorMessage = string.Empty;
            logger.Log(LogLevel.Info, "General preferences saved.");
            settingsApplied?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            ErrorMessage = e.Message;
            logger.Log(LogLevel.Error, $"Save general preferences failed: {e.Message}", e);
            return false;
        }
    }

    private void BrowsePythonDll()
    {
        var initialDirectory = GetInitialDirectory(PythonDllPath);
        var dialog = new RadOpenFileDialog
        {
            Owner = Application.Current.MainWindow,
            Multiselect = false,
            Filter = "Python DLL (*.dll)|*.dll|All files (*.*)|*.*",
            InitialDirectory = initialDirectory
        };

        FileDialogConfiguration.ConfigureFastFileDialog(dialog);
        FileDialogConfiguration.PrepareFileDialog(dialog, initialDirectory);
        dialog.ShowDialog();

        if (dialog.DialogResult == true)
        {
            PythonDllPath = dialog.FileName;
        }
    }

    private static string GetInitialDirectory(string filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                return directory;
            }
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }
}
