using System.Collections.ObjectModel;
using System.Globalization;
using Qenex.QInsight.EventAggregatorMsgs;
using Qenex.QInsight.ViewModels.ModelWrappers;
using Qenex.QInsight.ViewModels.SolutionExplorerWrappers;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.Variables.ValuePresentation;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QInsight.ViewModels;

public class VariablePropertiesViewModel : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    private readonly IVariableBase variable;
    private readonly IProtocolVariable? protocolVariable;
    private readonly Action refreshSource;
    private readonly IEnumerable<IPresentation> presentations;
    private readonly bool isReadOnly;
    private readonly Func<bool> getIsEditVariableEnabled;
    private readonly EventAggregator eventAggregator;

    public VariablePropertiesViewModel(
        EventAggregator ea,
        VariableSeWrapper variableWrapper,
        IEnumerable<IPresentation> presentations,
        IEnumerable<IVarEvent> varEvents,
        Func<bool> getIsEditVariableEnabled)
        : this(ea, variableWrapper.Variable, null, variableWrapper.Refresh, presentations, false, getIsEditVariableEnabled)
    {
        _ = varEvents;
    }

    public VariablePropertiesViewModel(
        EventAggregator ea,
        ProtocolVariableWrapper protocolVariableWrapper,
        IEnumerable<IPresentation> presentations,
        IEnumerable<IVarEvent> varEvents)
        : this(
            ea,
            protocolVariableWrapper.ProtocolVariable.Variable,
            protocolVariableWrapper.ProtocolVariable,
            protocolVariableWrapper.Refresh,
            presentations,
            true,
            () => false)
    {
        _ = varEvents;
    }

    private VariablePropertiesViewModel(
        EventAggregator ea,
        IVariableBase variable,
        IProtocolVariable? protocolVariable,
        Action refreshSource,
        IEnumerable<IPresentation> presentations,
        bool isReadOnly,
        Func<bool> getIsEditVariableEnabled)
    {
        eventAggregator = ea;
        this.variable = variable;
        this.protocolVariable = protocolVariable;
        this.refreshSource = refreshSource;
        this.presentations = presentations;
        this.isReadOnly = isReadOnly;
        this.getIsEditVariableEnabled = getIsEditVariableEnabled;
        Properties = CreateProperties();
    }

    public ObservableCollection<EditablePropertyWrapper> Properties { get; }

    private ObservableCollection<EditablePropertyWrapper> CreateProperties()
    {
        var properties = new ObservableCollection<EditablePropertyWrapper>
        {
            Create("Variable", "Id", () => variable.Id, value => variable.Id = Parse<int>(value)),
            Create("Variable", "Name", () => variable.Name, value => variable.Name = value),
            Create("Variable", "Label", () => variable.Label, value => variable.Label = value),
            Create("Variable", "Description", () => variable.Description, value => variable.Description = value)
        };

        if (variable is ScalarVariable scalarVariable)
        {
            properties.Add(Create("Scalar", "Size", () => scalarVariable.Size, value => scalarVariable.Size = Parse<int>(value)));
            properties.Add(Create(
                "Values",
                "Data Type",
                () => scalarVariable.Values.ValueType,
                value => SetScalarValueType(scalarVariable, value),
                ValuesGlobal.ValueDataTypeDict.Keys.Select(valueType => valueType.ToString())));
            properties.Add(Create("Values", "Value Size", () => scalarVariable.Values.Size, value => scalarVariable.Values.Size = Parse<int>(value)));
            properties.Add(Create("Values", "Length", () => scalarVariable.Values.Length, value => scalarVariable.Values.Length = Parse<int>(value)));
            properties.Add(Create(
                "Values",
                "Presentation",
                () => scalarVariable.Values.ValPresentation?.Name ?? string.Empty,
                value => SetPresentation(scalarVariable, value),
                presentations.Select(presentation => presentation.Name).Prepend(string.Empty)));
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
        return new EditablePropertyWrapper(group, name, getValue, setValue, RefreshVariableProperties, options, IsPropertyReadOnly);
    }

    private bool IsPropertyReadOnly()
    {
        return isReadOnly || !getIsEditVariableEnabled();
    }

    private void RefreshVariableProperties()
    {
        refreshSource();
        eventAggregator.Publish(new VariablePropertiesChangedMsg { Variable = variable });
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

    private void SetVariableValue(string value)
    {
        var currentValue = variable.GetValue();
        variable.SetValue(ConvertTo(value, currentValue?.GetType() ?? typeof(string)));

        if (protocolVariable == null)
        {
            return;
        }

        protocolVariable.NotifyValueChanged();
        _ = protocolVariable.NotifyValueChangedAsync();
    }

    private void SetScalarValueType(ScalarVariable scalarVariable, string value)
    {
        var valueType = Parse<ValuesGlobal.ValueDataType>(value);
        var currentValue = scalarVariable.Values.GetValue();
        var currentPresentation = scalarVariable.Values.ValPresentation;
        var currentLength = scalarVariable.Values.Length;

        var newValues = ValuesGlobal.CreateInstance(valueType);
        newValues.Length = currentLength;
        newValues.ValPresentation = currentPresentation;
        newValues.SetValue(ConvertTo(Convert.ToString(currentValue, CultureInfo.InvariantCulture) ?? string.Empty, GetSystemType(valueType)));
        scalarVariable.Values = newValues;
    }

    private void SetPresentation(ScalarVariable scalarVariable, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            scalarVariable.Values.ValPresentation = null!;
            return;
        }

        var presentation = presentations.FirstOrDefault(presentation =>
            presentation.Name.Equals(value, StringComparison.OrdinalIgnoreCase));

        scalarVariable.Values.ValPresentation = presentation
            ?? throw new InvalidOperationException($"Presentation \"{value}\" was not found.");
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

        if (targetType == typeof(DateTime))
        {
            return DateTime.Parse(value, CultureInfo.InvariantCulture);
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value, ignoreCase: true);
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static Type GetSystemType(ValuesGlobal.ValueDataType valueType)
    {
        var typeName = ValuesGlobal.ValueDataTypeDict[valueType];
        return Type.GetType(typeName) ?? throw new InvalidOperationException($"Type \"{typeName}\" was not found.");
    }
}
