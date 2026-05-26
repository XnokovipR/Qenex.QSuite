using System.Globalization;
using Qenex.QLibs.QUI;

namespace Qenex.QInsight.ViewModels.ModelWrappers;

public class EditablePropertyWrapper(
    string group,
    string name,
    Func<object?> getValue,
    Action<string> setValue,
    Action? afterSet = null,
    IEnumerable<string>? options = null,
    bool isReadOnly = false) : PropertyChangedBase
{
    public string Group => group;
    public string Name => name;
    public IReadOnlyList<string> Options { get; } = options?.ToList() ?? [];
    public bool HasOptions => Options.Count > 0;
    public bool IsReadOnly => isReadOnly;

    public string ValueText
    {
        get => Convert.ToString(getValue(), CultureInfo.InvariantCulture) ?? string.Empty;
        set
        {
            if (IsReadOnly)
            {
                return;
            }

            try
            {
                setValue(value);
                Error = string.Empty;
                afterSet?.Invoke();
                OnPropertyChanged();
            }
            catch (Exception e)
            {
                Error = e.Message;
                OnPropertyChanged(nameof(Error));
            }
        }
    }

    public string Error
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    } = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(Error);
}
