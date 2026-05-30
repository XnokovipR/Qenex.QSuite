using System.Collections.ObjectModel;
using System.Globalization;
using Qenex.QInsight.ViewModels.ModelWrappers;
using Qenex.QInsight.ViewModels.SolutionExplorerWrappers;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Variables.ValueConversion;

namespace Qenex.QInsight.ViewModels;

public class ConversionPropertiesViewModel : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    private readonly IValConversion conversion;
    private readonly Action refreshSource;
    private readonly Func<bool> getIsEditConversionEnabled;

    public ConversionPropertiesViewModel(
        EventAggregator ea,
        ConversionSeWrapper conversionWrapper,
        Func<bool> getIsEditConversionEnabled)
    {
        _ = ea;
        conversion = conversionWrapper.Conversion;
        refreshSource = conversionWrapper.Refresh;
        this.getIsEditConversionEnabled = getIsEditConversionEnabled;
        Properties = CreateProperties();
        EnumValues = CreateEnumValues();
    }

    public ObservableCollection<EditablePropertyWrapper> Properties { get; }
    public ObservableCollection<EnumConversionValueWrapper> EnumValues { get; }
    public bool IsEnumConversion => conversion is EnumValConversion;

    private ObservableCollection<EditablePropertyWrapper> CreateProperties()
    {
        var properties = new ObservableCollection<EditablePropertyWrapper>
        {
            Create("Conversion", "Name", () => conversion.Name, value => conversion.Name = value)
        };

        if (conversion is LinearValConversion linearConversion)
        {
            properties.Add(Create("Linear", "Multiplier", () => linearConversion.Multiplier, value => linearConversion.Multiplier = Parse<double>(value)));
            properties.Add(Create("Linear", "Offset", () => linearConversion.Offset, value => linearConversion.Offset = Parse<double>(value)));
        }
        else if (conversion is EnumValConversion enumConversion)
        {
            _ = enumConversion;
        }

        return properties;
    }

    private ObservableCollection<EnumConversionValueWrapper> CreateEnumValues()
    {
        if (conversion is not EnumValConversion enumConversion)
        {
            return [];
        }

        return new ObservableCollection<EnumConversionValueWrapper>(
            enumConversion.Enums.Select(enumValue => new EnumConversionValueWrapper(
                enumValue,
                refreshSource,
                IsPropertyReadOnly)));
    }

    private EditablePropertyWrapper Create(
        string group,
        string name,
        Func<object?> getValue,
        Action<string> setValue)
    {
        return new EditablePropertyWrapper(group, name, getValue, setValue, refreshSource, null, IsPropertyReadOnly);
    }

    private bool IsPropertyReadOnly()
    {
        return !getIsEditConversionEnabled();
    }

    public void RefreshReadOnly()
    {
        foreach (var property in Properties)
        {
            property.RefreshReadOnly();
        }

        foreach (var enumValue in EnumValues)
        {
            enumValue.RefreshReadOnly();
        }
    }

    public void RefreshProperties()
    {
        foreach (var property in Properties)
        {
            property.RefreshValue();
        }

        RefreshEnumValues();
    }

    private void RefreshEnumValues()
    {
        EnumValues.Clear();

        if (conversion is not EnumValConversion enumConversion)
        {
            return;
        }

        foreach (var enumValue in enumConversion.Enums)
        {
            EnumValues.Add(new EnumConversionValueWrapper(
                enumValue,
                refreshSource,
                IsPropertyReadOnly));
        }
    }

    private static T Parse<T>(string value)
    {
        return (T)ConvertTo(value, typeof(T));
    }

    private static object ConvertTo(string value, Type targetType)
    {
        if (targetType == typeof(string))
        {
            return value;
        }

        if (targetType == typeof(bool))
        {
            return bool.Parse(value);
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value, ignoreCase: true);
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }
}

public class EnumConversionValueWrapper(
    EnumType enumValue,
    Action refreshSource,
    Func<bool> getIsReadOnly) : PropertyChangedBase
{
    public string Name
    {
        get => enumValue.Name;
        set
        {
            if (IsReadOnly)
            {
                return;
            }

            enumValue.Name = value;
            Error = string.Empty;
            refreshSource();
            OnPropertyChanged();
        }
    }

    public string Value
    {
        get => Convert.ToString(enumValue.Value, CultureInfo.InvariantCulture) ?? string.Empty;
        set
        {
            if (IsReadOnly)
            {
                return;
            }

            try
            {
                enumValue.Value = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                Error = string.Empty;
                refreshSource();
                OnPropertyChanged();
            }
            catch (Exception e)
            {
                Error = e.Message;
            }
        }
    }

    public bool IsReadOnly => getIsReadOnly();
    public bool IsEditable => !IsReadOnly;

    public string Error
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public void RefreshReadOnly()
    {
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(IsEditable));
    }
}
