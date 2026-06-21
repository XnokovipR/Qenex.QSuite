using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.Variables.ValuePresentation;

namespace Qenex.QInsight.ViewModels.ModelWrappers;

public class ProjectConfigurationVariableWrapper : PropertyChangedBase
{
    private readonly IEnumerable<IPresentation> presentations;
    private readonly List<EditablePropertyWrapper> editableProperties = [];

    private VariableState originalState;
    private VariableState currentState;

    private EditablePropertyWrapper? presentationProperty;

    public ProjectConfigurationVariableWrapper(IVariableBase variable, IEnumerable<IPresentation> presentations, bool isNew = false)
    {
        Variable = variable;
        this.presentations = presentations;
        IsNew = isNew;
        originalState = VariableState.FromVariable(variable);
        currentState = originalState;
        Properties = CreateProperties();
    }

    public IVariableBase Variable { get; }
    public bool IsNew { get; }

    public ObservableCollection<EditablePropertyWrapper> Properties { get; }
    public int Id => currentState.Id;

    public string DisplayName => $"{currentState.Label} ({currentState.Id})";

    public bool HasChanges => !currentState.Equals(originalState);

    public void ApplyChanges()
    {
        Variable.Id = currentState.Id;
        Variable.Namespace = currentState.Namespace;
        Variable.Name = currentState.Name;
        Variable.Label = currentState.Label;
        Variable.Description = currentState.Description;

        if (Variable is ScalarVariable scalarVariable && currentState.ScalarState != null)
        {
            ApplyScalarState(scalarVariable, currentState.ScalarState);
        }

        originalState = currentState;
        NotifyStateChanged();
    }

    public void CancelChanges()
    {
        currentState = originalState;
        RefreshProperties();
        NotifyStateChanged();
    }

    public void RefreshPresentationOptions(IEnumerable<string> presentationNames)
    {
        presentationProperty?.SetOptions(presentationNames.Prepend(string.Empty));
    }

    private ObservableCollection<EditablePropertyWrapper> CreateProperties()
    {
        var properties = new ObservableCollection<EditablePropertyWrapper>
        {
            Create("Variable", "Id", () => currentState.Id, value => UpdateState(currentState with { Id = Parse<int>(value) })),
            Create("Variable", "Namespace", () => currentState.Namespace, value => UpdateState(currentState with { Namespace = value })),
            Create("Variable", "Name", () => currentState.Name, value => UpdateState(currentState with { Name = value })),
            Create("Variable", "Label", () => currentState.Label, value => UpdateState(currentState with { Label = value })),
            Create("Variable", "Description", () => currentState.Description, value => UpdateState(currentState with { Description = value }))
        };

        if (currentState.ScalarState != null)
        {
            // Size and Value Size are not editable: they are derived from Data Type and Length on apply.
            properties.Add(Create(
                "Values",
                "Data Type",
                () => currentState.ScalarState.ValueType,
                value => UpdateScalarState(s => s with { ValueType = Parse<ValuesGlobal.ValueDataType>(value) }),
                ValuesGlobal.ValueDataTypeDict.Keys.Select(valueType => valueType.ToString())));
            properties.Add(Create("Values", "Length", () => currentState.ScalarState.Length, value => UpdateScalarState(s => s with { Length = Parse<int>(value) })));
            presentationProperty = Create(
                "Values",
                "Presentation",
                () => currentState.ScalarState.PresentationName,
                value => UpdateScalarState(s => s with { PresentationName = value }),
                presentations.Select(presentation => presentation.Name).Prepend(string.Empty));
            properties.Add(presentationProperty);
        }

        foreach (var property in properties)
        {
            editableProperties.Add(property);
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
        return new EditablePropertyWrapper(group, name, getValue, setValue, null, options);
    }

    private void UpdateScalarState(Func<ScalarVariableState, ScalarVariableState> update)
    {
        if (currentState.ScalarState == null)
        {
            return;
        }

        UpdateState(currentState with { ScalarState = update(currentState.ScalarState) });
    }

    private void UpdateState(VariableState state)
    {
        currentState = state;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(HasChanges));
    }

    private void RefreshProperties()
    {
        foreach (var property in editableProperties)
        {
            property.RefreshValue();
        }
    }

    private void ApplyScalarState(ScalarVariable scalarVariable, ScalarVariableState state)
    {
        if (scalarVariable.Values.ValueType != state.ValueType)
        {
            var currentValue = scalarVariable.Values.GetValue();
            var newValues = ValuesGlobal.CreateInstance(state.ValueType);

            // Migrate the existing value to the new type; if it does not fit (e.g. a large uint32
            // switched to ushort) keep the new type's default rather than crashing on overflow.
            if (TryConvertTo(Convert.ToString(currentValue, CultureInfo.InvariantCulture) ?? string.Empty,
                    GetSystemType(state.ValueType), out var convertedValue))
            {
                newValues.SetValue(convertedValue);
            }

            scalarVariable.Values = newValues;
        }

        scalarVariable.Values.Length = state.Length;

        // Size and Value Size are derived (not user-entered) and stored on the variable / serialized to
        // XML from here: value size = byte width of the type, total size = value size * element count.
        scalarVariable.Values.Size = GetValueSize(state.ValueType);
        scalarVariable.Size = scalarVariable.Values.Size * Math.Max(state.Length, 1);

        scalarVariable.Values.ValPresentation = string.IsNullOrWhiteSpace(state.PresentationName)
            ? null!
            : presentations.First(presentation => presentation.Name.Equals(state.PresentationName, StringComparison.OrdinalIgnoreCase));
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

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value, ignoreCase: true);
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    // Best-effort conversion used when changing a variable's data type: returns false instead of
    // throwing when the value cannot be represented in the target type (overflow / bad format).
    private static bool TryConvertTo(string value, Type targetType, out object converted)
    {
        try
        {
            converted = ConvertTo(value, targetType);
            return true;
        }
        catch (Exception exception) when (exception is OverflowException or FormatException or InvalidCastException)
        {
            converted = null!;
            return false;
        }
    }

    private static Type GetSystemType(ValuesGlobal.ValueDataType valueType)
    {
        var typeName = ValuesGlobal.ValueDataTypeDict[valueType];
        return Type.GetType(typeName) ?? throw new InvalidOperationException($"Type \"{typeName}\" was not found.");
    }

    // Byte width of a single value of the given type, derived from the type itself (String has none).
    private static int GetValueSize(ValuesGlobal.ValueDataType valueType)
    {
        return valueType == ValuesGlobal.ValueDataType.String ? 0 : Marshal.SizeOf(GetSystemType(valueType));
    }

    private sealed record VariableState(
        int Id,
        string Namespace,
        string Name,
        string Label,
        string Description,
        ScalarVariableState? ScalarState)
    {
        public static VariableState FromVariable(IVariableBase variable)
        {
            return new VariableState(
                variable.Id,
                variable.Namespace,
                variable.Name,
                variable.Label,
                variable.Description,
                variable is ScalarVariable scalarVariable
                    ? ScalarVariableState.FromScalarVariable(scalarVariable)
                    : null);
        }
    }

    private sealed record ScalarVariableState(
        ValuesGlobal.ValueDataType ValueType,
        int Length,
        string PresentationName)
    {
        public static ScalarVariableState FromScalarVariable(ScalarVariable scalarVariable)
        {
            return new ScalarVariableState(
                scalarVariable.Values.ValueType,
                scalarVariable.Values.Length,
                scalarVariable.Values.ValPresentation?.Name ?? string.Empty);
        }
    }
}
