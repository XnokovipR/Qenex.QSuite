using System.Collections.ObjectModel;
using System.Globalization;
using Qenex.QInsight.ViewModels.ModelWrappers;
using Qenex.QInsight.ViewModels.SolutionExplorerWrappers;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QInsight.ViewModels;

public class EventPropertiesViewModel : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    private readonly IVarEvent variableEvent;
    private readonly Action refreshSource;
    private readonly Func<bool> getIsEditEventEnabled;

    public EventPropertiesViewModel(
        EventAggregator ea,
        IVariableEventSeWrapper eventWrapper,
        Func<bool> getIsEditEventEnabled)
    {
        _ = ea;
        variableEvent = eventWrapper.VariableEvent;
        refreshSource = eventWrapper.Refresh;
        this.getIsEditEventEnabled = getIsEditEventEnabled;
        Properties = CreateProperties();
    }

    public ObservableCollection<EditablePropertyWrapper> Properties { get; }
    public string EventTypeTitle => variableEvent switch
    {
        OnRequestVarEvent => "On Request Event",
        OnValueChangedVarEvent => "On Value Changed Event",
        PeriodicVarEvent => "Periodic Event",
        _ => variableEvent.GetType().Name
    };

    private ObservableCollection<EditablePropertyWrapper> CreateProperties()
    {
        var properties = new ObservableCollection<EditablePropertyWrapper>
        {
            Create("Event", "Name", () => variableEvent.Name, value => variableEvent.Name = value)
        };

        if (variableEvent is PeriodicVarEvent periodicVarEvent)
        {
            properties.Add(Create("Periodic", "Period", () => periodicVarEvent.Period, value => periodicVarEvent.Period = Parse<int>(value)));
            properties.Add(Create(
                "Periodic",
                "Unit",
                () => periodicVarEvent.Unit,
                value => periodicVarEvent.Unit = Parse<TimeUnit>(value),
                Enum.GetNames<TimeUnit>()));
        }
        else if (variableEvent is OnValueChangedVarEvent onValueChangedVarEvent)
        {
            properties.Add(Create("On Change", "Threshold", () => onValueChangedVarEvent.Threshold, value => onValueChangedVarEvent.Threshold = Parse<double>(value)));
        }

        return properties;
    }

    private EditablePropertyWrapper Create(
        string group,
        string name,
        Func<object?> getValue,
        Action<string> setValue,
        IEnumerable<string>? options = null)
    {
        return new EditablePropertyWrapper(group, name, getValue, setValue, refreshSource, options, IsPropertyReadOnly);
    }

    private bool IsPropertyReadOnly()
    {
        return !getIsEditEventEnabled();
    }

    public void RefreshReadOnly()
    {
        foreach (var property in Properties)
        {
            property.RefreshReadOnly();
        }
    }

    public void RefreshProperties()
    {
        foreach (var property in Properties)
        {
            property.RefreshValue();
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
