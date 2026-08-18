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
    private Func<bool>? getIsReadOnly;

    public EditablePropertyWrapper(
        string group,
        string name,
        Func<object?> getValue,
        Action<string> setValue,
        Action? afterSet,
        IEnumerable<string>? options,
        Func<bool> getIsReadOnly)
        : this(group, name, getValue, setValue, afterSet, options)
    {
        this.getIsReadOnly = getIsReadOnly;
    }

    public string Group => group;
    public string Name => name;
    public IReadOnlyList<string> Options { get; private set; } = options?.ToList() ?? [];
    public bool HasOptions => Options.Count > 0;
    public bool IsReadOnly => getIsReadOnly?.Invoke() ?? isReadOnly;
    public bool IsEditable => !IsReadOnly;

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

    /// <summary>
    /// Optional selector shown in front of the value box on the same row (e.g. a shift direction
    /// combo before the bit count). Absent unless configured via SetPrefix.
    /// </summary>
    public IReadOnlyList<string> PrefixOptions { get; private set; } = [];
    public bool HasPrefixOptions => PrefixOptions.Count > 0;

    private Func<string>? getPrefix;
    private Action<string>? setPrefix;

    public string PrefixText
    {
        get => getPrefix?.Invoke() ?? string.Empty;
        set
        {
            if (IsReadOnly || setPrefix == null)
            {
                return;
            }

            try
            {
                setPrefix(value);
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

    /// <summary>
    /// True = the value is pushed to the model (and validated) only when the field loses focus,
    /// so free-form input such as "0x0F" is not rejected half-typed. Default = on every keystroke.
    /// </summary>
    public bool UpdateOnLostFocus { get; private set; }

    public EditablePropertyWrapper SetUpdateOnLostFocus()
    {
        UpdateOnLostFocus = true;
        OnPropertyChanged(nameof(UpdateOnLostFocus));
        return this;
    }

    public EditablePropertyWrapper SetPrefix(IEnumerable<string> prefixOptions, Func<string> getPrefixValue, Action<string> setPrefixValue)
    {
        PrefixOptions = prefixOptions.ToList();
        getPrefix = getPrefixValue;
        setPrefix = setPrefixValue;
        OnPropertyChanged(nameof(PrefixOptions));
        OnPropertyChanged(nameof(HasPrefixOptions));
        return this;
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

    public void RefreshReadOnly()
    {
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(IsEditable));
    }

    public void RefreshValue()
    {
        OnPropertyChanged(nameof(ValueText));
        OnPropertyChanged(nameof(PrefixText));
    }

    public void SetOptions(IEnumerable<string> newOptions)
    {
        Options = newOptions.ToList();
        OnPropertyChanged(nameof(Options));
        OnPropertyChanged(nameof(HasOptions));
    }
}
