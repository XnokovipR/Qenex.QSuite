using Qenex.QLibs.QUI;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.ViewModels;

public enum ProjectPasswordAction
{
    None,
    Set,
    Remove
}

/// <summary>
/// Backs the ribbon "Password" dialog. Without a password it offers Set (new +
/// confirm); with one it offers Change (current + new + confirm) and Remove
/// (current). Buttons enable once their fields are filled; mismatched or wrong
/// passwords show an error that clears on the next edit. The chosen action is
/// applied by the caller on the next save.
/// </summary>
public class ProjectPasswordViewModel : PropertyChangedBase
{
    private readonly string? currentProjectPassword;
    private RadWindow? parentWindow;
    private string currentPassword = string.Empty;
    private string newPassword = string.Empty;
    private string confirmPassword = string.Empty;
    private string errorMessage = string.Empty;

    public ProjectPasswordViewModel(string? projectPassword)
    {
        currentProjectPassword = projectPassword;
        SetCommand = new RelayCommand<object>(_ => ApplySet(), _ => CanApplySet());
        RemoveCommand = new RelayCommand<object>(_ => ApplyRemove(), _ => CanApplyRemove());
        CancelCommand = new RelayCommand<object>(_ => parentWindow?.Close());
    }

    public bool HasPassword => currentProjectPassword != null;

    public string Title => HasPassword
        ? "This project is password protected."
        : "This project is not password protected.";

    public string ApplyButtonText => HasPassword ? "Change password" : "Set password";

    public const string LossWarning =
        "Keep the password safe: a project with a lost password cannot be opened or recovered, "
        + "not even by QENEX. The password takes effect when the project is saved.";

    public string CurrentPassword
    {
        get => currentPassword;
        set
        {
            currentPassword = value;
            OnFieldEdited(nameof(CurrentPassword));
        }
    }

    public string NewPassword
    {
        get => newPassword;
        set
        {
            newPassword = value;
            OnFieldEdited(nameof(NewPassword));
        }
    }

    public string ConfirmPassword
    {
        get => confirmPassword;
        set
        {
            confirmPassword = value;
            OnFieldEdited(nameof(ConfirmPassword));
        }
    }

    public string ErrorMessage
    {
        get => errorMessage;
        private set
        {
            errorMessage = value;
            OnPropertyChanged(nameof(ErrorMessage));
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => ErrorMessage.Length > 0;

    public ProjectPasswordAction Action { get; private set; } = ProjectPasswordAction.None;

    /// <summary>The new project password when Action is Set.</summary>
    public string? ResultPassword { get; private set; }

    public RelayCommand<object> SetCommand { get; }
    public RelayCommand<object> RemoveCommand { get; }
    public RelayCommand<object> CancelCommand { get; }

    public void SetParentWindow(RadWindow window)
    {
        parentWindow = window;
    }

    private void OnFieldEdited(string propertyName)
    {
        OnPropertyChanged(propertyName);
        ErrorMessage = string.Empty;
        SetCommand.OnCanExecuteChanged();
        RemoveCommand.OnCanExecuteChanged();
    }

    private bool CanApplySet()
    {
        return NewPassword.Length > 0
               && ConfirmPassword.Length > 0
               && (!HasPassword || CurrentPassword.Length > 0);
    }

    private bool CanApplyRemove()
    {
        return CurrentPassword.Length > 0;
    }

    private void ApplySet()
    {
        if (!ValidateCurrentPassword())
        {
            return;
        }

        if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
        {
            ErrorMessage = "The new password and its confirmation do not match.";
            return;
        }

        Action = ProjectPasswordAction.Set;
        ResultPassword = NewPassword;
        parentWindow?.Close();
    }

    private void ApplyRemove()
    {
        if (!ValidateCurrentPassword())
        {
            return;
        }

        Action = ProjectPasswordAction.Remove;
        parentWindow?.Close();
    }

    private bool ValidateCurrentPassword()
    {
        if (!HasPassword || string.Equals(CurrentPassword, currentProjectPassword, StringComparison.Ordinal))
        {
            return true;
        }

        ErrorMessage = "The current password is incorrect.";
        return false;
    }
}
