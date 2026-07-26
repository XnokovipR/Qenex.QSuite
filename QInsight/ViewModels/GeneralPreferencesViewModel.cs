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
    private RadWindow? parentWindow;

    public GeneralPreferencesViewModel(AppSettings appSettings, Logger logger)
    {
        this.appSettings = appSettings;
        this.logger = logger;

        PythonDllPath = appSettings.ScriptEngine.PythonDllPath;

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

    public string PythonDllPath
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
        try
        {
            appSettings.ScriptEngine.PythonDllPath = PythonDllPath.Trim();

            AppSettings.SaveAppSettingsToFile(AppDataPaths.AppSettingsFile, appSettings);
            ErrorMessage = string.Empty;
            logger.Log(LogLevel.Info, "General preferences saved.");
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
