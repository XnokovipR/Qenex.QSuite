using Qenex.QLibs.QUI;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.ViewModels;

public enum CloseProjectDecision
{
    Cancel,
    SaveAndClose,
    CloseWithoutSaving
}

/// <summary>Backs the three-way prompt shown before an open project is closed
/// (New/Open with a project open). Closing the window any other way means Cancel.</summary>
public class CloseProjectPromptViewModel : PropertyChangedBase
{
    private RadWindow? parentWindow;

    public CloseProjectPromptViewModel(string message)
    {
        Message = message;
        SaveAndCloseCommand = new RelayCommand<object>(_ => CloseWith(CloseProjectDecision.SaveAndClose));
        CloseWithoutSavingCommand = new RelayCommand<object>(_ => CloseWith(CloseProjectDecision.CloseWithoutSaving));
        CancelCommand = new RelayCommand<object>(_ => CloseWith(CloseProjectDecision.Cancel));
    }

    public string Message { get; }

    public CloseProjectDecision Decision { get; private set; } = CloseProjectDecision.Cancel;

    public RelayCommand<object> SaveAndCloseCommand { get; }
    public RelayCommand<object> CloseWithoutSavingCommand { get; }
    public RelayCommand<object> CancelCommand { get; }

    public void SetParentWindow(RadWindow window)
    {
        parentWindow = window;
    }

    private void CloseWith(CloseProjectDecision decision)
    {
        Decision = decision;
        parentWindow?.Close();
    }
}
