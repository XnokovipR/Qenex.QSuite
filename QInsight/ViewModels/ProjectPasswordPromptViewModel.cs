using Qenex.QLibs.QUI;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.ViewModels;

/// <summary>Backs the password prompt shown when a password-protected project is
/// opened. Closing the window any other way than OK means cancelling the open.</summary>
public class ProjectPasswordPromptViewModel : PropertyChangedBase
{
    private RadWindow? parentWindow;
    private string password = string.Empty;

    public ProjectPasswordPromptViewModel(string fileName, bool isRetry)
    {
        Message = $"Project \"{fileName}\" is password protected.\nEnter the password to open it.";
        ErrorMessage = isRetry ? "The password is incorrect (or the file is damaged)." : string.Empty;
        OkCommand = new RelayCommand<object>(_ => Confirm(), _ => Password.Length > 0);
        CancelCommand = new RelayCommand<object>(_ => parentWindow?.Close());
    }

    public string Message { get; }
    public string ErrorMessage { get; }
    public bool HasError => ErrorMessage.Length > 0;

    public string Password
    {
        get => password;
        set
        {
            password = value;
            OnPropertyChanged(nameof(Password));
            OkCommand.OnCanExecuteChanged();
        }
    }

    /// <summary>Null until OK is pressed with a non-empty password.</summary>
    public string? EnteredPassword { get; private set; }

    public RelayCommand<object> OkCommand { get; }
    public RelayCommand<object> CancelCommand { get; }

    public void SetParentWindow(RadWindow window)
    {
        parentWindow = window;
    }

    private void Confirm()
    {
        EnteredPassword = Password;
        parentWindow?.Close();
    }
}
