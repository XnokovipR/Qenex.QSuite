using Qenex.QInsight.Helpers;
using Qenex.QLibs.QUI;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.ViewModels;

public enum PythonSetupDecision
{
    Close,
    OpenPreferences,
    OpenDocumentation
}

/// <summary>Backs the prompt shown when a runtime/replay start needs Python that is not set up
/// (scripting disabled while the project has enabled scripts, or the DLL path missing).
/// The message names the concrete problem; the hint tells how to get Python. The two action
/// buttons lead straight to Preferences or to the installation chapter of the documentation.</summary>
public class PythonSetupPromptViewModel : PropertyChangedBase
{
    private RadWindow? parentWindow;

    public PythonSetupPromptViewModel(string message)
    {
        Message = message;
        OpenPreferencesCommand = new RelayCommand<object>(_ => CloseWith(PythonSetupDecision.OpenPreferences));
        OpenDocumentationCommand = new RelayCommand<object>(_ => CloseWith(PythonSetupDecision.OpenDocumentation));
        CloseCommand = new RelayCommand<object>(_ => CloseWith(PythonSetupDecision.Close));
    }

    public string Message { get; }

    public string Hint { get; } =
        $"Python is not part of QInsight. Install 64-bit CPython {PythonInstallationLocator.SupportedRangeText} " +
        "from python.org, then open Options -> Preferences -> General, tick 'Use Python scripts' and set the " +
        "Python DLL path (the Detect button finds an installed Python for you).";

    public PythonSetupDecision Decision { get; private set; } = PythonSetupDecision.Close;

    public RelayCommand<object> OpenPreferencesCommand { get; }
    public RelayCommand<object> OpenDocumentationCommand { get; }
    public RelayCommand<object> CloseCommand { get; }

    public void SetParentWindow(RadWindow window)
    {
        parentWindow = window;
    }

    private void CloseWith(PythonSetupDecision decision)
    {
        Decision = decision;
        parentWindow?.Close();
    }
}
