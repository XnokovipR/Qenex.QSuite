using System.Globalization;
using System.Reflection;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Scripting.Script;
using Qenex.QSuite.Scripting.ScriptingEngine;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QInsight.ViewModels.ModelWrappers;

public class ProjectConfigurationProtocolVariableWrapper : PropertyChangedBase
{
    private const string DefaultScriptAdditionalInfo = "threshold=1000.0";
    private readonly IEnumerable<ProjectConfigurationProtocolOption> sourceOptions;
    private readonly IEnumerable<IVarEvent> variableEvents;
    private readonly Func<IEnumerable<string>>? variableEventOptionsProvider;
    private readonly IEnumerable<ProjectConfigurationScriptWrapper> scripts;
    private readonly OnValueChangedScriptTrigger? originalScriptTrigger;
    private ProjectConfigurationProtocolOption originalSource;
    private ProjectConfigurationProtocolOption selectedSource;
    private string originalCommParam;
    private bool originalIsCommunicated;
    private bool originalIsFileLogEnabled;
    private string originalSelectedScriptFileName;
    private string originalScriptAdditionalInfo;
    private string commParam;
    private bool isCommunicated;
    private bool isFileLogEnabled;
    private Dictionary<string, string> additionalCommParameters = new(StringComparer.OrdinalIgnoreCase);
    private string selectedVariableEventName = string.Empty;
    private CommDirection selectedDirection = CommDirection.Read;
    private int multiplier = 1;
    private string communicationId = string.Empty;
    private string selectedScriptFileName = string.Empty;
    private string scriptAdditionalInfo = string.Empty;

    public ProjectConfigurationProtocolVariableWrapper(
        IProtocolVariable protocolVariable,
        ProjectConfigurationProtocolOption source,
        IEnumerable<ProjectConfigurationProtocolOption> sourceOptions,
        IEnumerable<IVarEvent> variableEvents,
        IEnumerable<ProjectConfigurationScriptWrapper> scripts,
        OnValueChangedScriptTrigger? scriptTrigger,
        bool isFileLogEnabled,
        Func<IEnumerable<string>>? variableEventOptionsProvider = null)
    {
        ProtocolVariable = protocolVariable;
        originalSource = source;
        selectedSource = source;
        this.sourceOptions = sourceOptions;
        this.variableEvents = variableEvents;
        this.variableEventOptionsProvider = variableEventOptionsProvider;
        this.scripts = scripts;
        originalScriptTrigger = scriptTrigger;
        originalCommParam = ProjectConfigurationProtocolVariableFactory.GetCommParam(protocolVariable.ProtocolVariableSpecification);
        originalIsCommunicated = protocolVariable.IsCommunicated;
        originalIsFileLogEnabled = isFileLogEnabled;
        originalSelectedScriptFileName = scriptTrigger?.ScriptFileName ?? string.Empty;
        originalScriptAdditionalInfo = scriptTrigger?.AdditionalInfo ?? string.Empty;
        commParam = originalCommParam;
        isCommunicated = originalIsCommunicated;
        this.isFileLogEnabled = originalIsFileLogEnabled;
        selectedScriptFileName = originalSelectedScriptFileName;
        scriptAdditionalInfo = originalScriptAdditionalInfo;
        ReadCommunicationFieldsFromCommParam();
    }

    public IProtocolVariable ProtocolVariable { get; private set; }
    public IVariableBase Variable => ProtocolVariable.Variable;
    public IEnumerable<ProjectConfigurationProtocolOption> SourceOptions => sourceOptions;
    public IEnumerable<string> VariableEventOptions => (variableEventOptionsProvider?.Invoke() ?? variableEvents.Select(variableEvent => variableEvent.Name))
        .Prepend(string.Empty);
    public IEnumerable<CommDirection> DirectionOptions => Enum.GetValues<CommDirection>();
    public IEnumerable<string> ScriptOptions => scripts
        .Where(script => script.ExecutionMode == ScriptExecutionMode.OnValueChanged)
        .Select(script => script.FileName)
        .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Prepend(string.Empty);

    public string DisplayName => $"{Variable.Label} ({Variable.Id})";
    public string SourceText => $"{SelectedSource.DriverLabel} / {SelectedSource.ProtocolLabel}";

    public ProjectConfigurationProtocolOption SelectedSource
    {
        get => selectedSource;
        set
        {
            if (selectedSource == value)
            {
                return;
            }

            selectedSource = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SourceText));
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public string CommParam
    {
        get => commParam;
        set
        {
            if (commParam == value)
            {
                return;
            }

            commParam = value;
            ReadCommunicationFieldsFromCommParam();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public string SelectedVariableEventName
    {
        get => selectedVariableEventName;
        set
        {
            if (selectedVariableEventName == value)
            {
                return;
            }

            selectedVariableEventName = value;
            UpdateCommParamFromFields();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public CommDirection SelectedDirection
    {
        get => selectedDirection;
        set
        {
            if (selectedDirection == value)
            {
                return;
            }

            selectedDirection = value;
            UpdateCommParamFromFields();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public int Multiplier
    {
        get => multiplier;
        set
        {
            if (multiplier == value)
            {
                return;
            }

            multiplier = value;
            UpdateCommParamFromFields();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public string CommunicationId
    {
        get => communicationId;
        set
        {
            if (communicationId == value)
            {
                return;
            }

            communicationId = value;
            UpdateCommParamFromFields();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public bool IsCommunicated
    {
        get => isCommunicated;
        set
        {
            if (isCommunicated == value)
            {
                return;
            }

            isCommunicated = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public bool IsFileLogEnabled
    {
        get => isFileLogEnabled;
        set
        {
            if (isFileLogEnabled == value)
            {
                return;
            }

            isFileLogEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public string SelectedScriptFileName
    {
        get => selectedScriptFileName;
        set
        {
            if (selectedScriptFileName == value)
            {
                return;
            }

            selectedScriptFileName = value;
            ScriptAdditionalInfo = string.IsNullOrWhiteSpace(selectedScriptFileName)
                ? string.Empty
                : DefaultScriptAdditionalInfo;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public string ScriptAdditionalInfo
    {
        get => scriptAdditionalInfo;
        set
        {
            if (scriptAdditionalInfo == value)
            {
                return;
            }

            scriptAdditionalInfo = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public bool HasChanges =>
        SelectedSource != originalSource
        || CommParam != originalCommParam
        || IsCommunicated != originalIsCommunicated
        || IsFileLogEnabled != originalIsFileLogEnabled
        || SelectedScriptFileName != originalSelectedScriptFileName
        || ScriptAdditionalInfo != originalScriptAdditionalInfo;

    public void ApplyChanges()
    {
        if (SelectedSource != originalSource
            || CommParam != originalCommParam
            || IsCommunicated != originalIsCommunicated)
        {
            var protocolVariable = ProjectConfigurationProtocolVariableFactory.CreateProtocolVariable(
                SelectedSource.Protocol,
                Variable,
                variableEvents,
                CommParam,
                IsCommunicated);

            originalSource.Protocol.RemoveProtocolVariable(ProtocolVariable);
            SelectedSource.Protocol.AddVariable(protocolVariable);
            ProtocolVariable = protocolVariable;
            OnPropertyChanged(nameof(ProtocolVariable));
        }

        originalSource = SelectedSource;
        originalCommParam = CommParam;
        originalIsCommunicated = IsCommunicated;
        originalIsFileLogEnabled = IsFileLogEnabled;
        originalSelectedScriptFileName = SelectedScriptFileName;
        originalScriptAdditionalInfo = ScriptAdditionalInfo;
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(SourceText));
        OnPropertyChanged(nameof(HasChanges));
    }

    public void CancelChanges()
    {
        SelectedSource = originalSource;
        CommParam = originalCommParam;
        IsCommunicated = originalIsCommunicated;
        IsFileLogEnabled = originalIsFileLogEnabled;
        SelectedScriptFileName = originalSelectedScriptFileName;
        ScriptAdditionalInfo = originalScriptAdditionalInfo;
        OnPropertyChanged(nameof(HasChanges));
    }

    public void ApplyScriptTrigger(IList<OnValueChangedScriptTrigger> triggers)
    {
        if (originalScriptTrigger != null)
        {
            triggers.Remove(originalScriptTrigger);
        }

        foreach (var trigger in triggers.Where(trigger => trigger.VariableId == Variable.Id).ToList())
        {
            triggers.Remove(trigger);
        }

        if (IsCommunicated && !string.IsNullOrWhiteSpace(SelectedScriptFileName))
        {
            triggers.Add(new OnValueChangedScriptTrigger(Variable.Id, SelectedScriptFileName, ScriptAdditionalInfo));
        }
    }

    public void RefreshScriptOptions()
    {
        OnPropertyChanged(nameof(ScriptOptions));
    }

    public void RefreshVariableEventOptions()
    {
        OnPropertyChanged(nameof(VariableEventOptions));
    }

    public void RefreshSourceOptions()
    {
        OnPropertyChanged(nameof(SourceOptions));
    }

    private void ReadCommunicationFieldsFromCommParam()
    {
        var parameters = ProjectConfigurationProtocolVariableFactory.ParseCommParam(CommParam);

        selectedVariableEventName = parameters.TryGetValue("eventRef", out var eventRef)
            ? eventRef
            : string.Empty;
        selectedDirection = parameters.TryGetValue("direction", out var direction)
                            && Enum.TryParse<CommDirection>(direction, ignoreCase: true, out var parsedDirection)
            ? parsedDirection
            : CommDirection.Read;
        multiplier = parameters.TryGetValue("multiplier", out var multiplierText)
                     && int.TryParse(multiplierText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedMultiplier)
            ? parsedMultiplier
            : 1;
        communicationId = parameters.TryGetValue("id", out var id)
            ? id
            : string.Empty;
        additionalCommParameters = parameters
            .Where(parameter => !IsKnownCommunicationField(parameter.Key))
            .ToDictionary(parameter => parameter.Key, parameter => parameter.Value, StringComparer.OrdinalIgnoreCase);

        OnPropertyChanged(nameof(SelectedVariableEventName));
        OnPropertyChanged(nameof(SelectedDirection));
        OnPropertyChanged(nameof(Multiplier));
        OnPropertyChanged(nameof(CommunicationId));
    }

    private void UpdateCommParamFromFields()
    {
        var parameters = new List<string>
        {
            $"direction=\"{SelectedDirection.ToString().ToLowerInvariant()}\""
        };

        if (!string.IsNullOrWhiteSpace(SelectedVariableEventName))
        {
            parameters.Add($"eventRef=\"{SelectedVariableEventName}\"");
        }

        parameters.Add($"multiplier=\"{Multiplier}\"");
        parameters.Add($"id=\"{CommunicationId}\"");
        parameters.AddRange(additionalCommParameters.Select(parameter => $"{parameter.Key}=\"{parameter.Value}\""));
        commParam = string.Join(";", parameters);
        OnPropertyChanged(nameof(CommParam));
    }

    private static bool IsKnownCommunicationField(string name)
    {
        return name.Equals("direction", StringComparison.OrdinalIgnoreCase)
               || name.Equals("eventRef", StringComparison.OrdinalIgnoreCase)
               || name.Equals("multiplier", StringComparison.OrdinalIgnoreCase)
               || name.Equals("id", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record ProjectConfigurationProtocolOption(
    IDriverBase Driver,
    IProtocolBase Protocol)
{
    public string DriverLabel => string.IsNullOrWhiteSpace(Driver.Label)
        ? Driver.Specification.Label
        : Driver.Label;

    public string ProtocolLabel => string.IsNullOrWhiteSpace(Protocol.Specification.Label)
        ? Protocol.Specification.Name
        : Protocol.Specification.Label;

    public string DisplayName => $"{DriverLabel} / {ProtocolLabel}";
}

public static class ProjectConfigurationProtocolVariableFactory
{
    private const string PiZeroJsonProtocolName = "PiZeroJsonProtocol";

    public static IProtocolVariable CreateProtocolVariable(
        IProtocolBase protocol,
        IVariableBase variable,
        IEnumerable<IVarEvent> variableEvents,
        string commParam,
        bool isCommunicated)
    {
        IProtocolVariable? protocolVariable = null;

        try
        {
            protocolVariable = protocol.CreateProtocolVariable(variable, variableEvents, commParam, isCommunicated);
        }
        catch
        {
        }

        if (protocolVariable == null)
        {
            try
            {
                protocolVariable = protocol.CreateProtocolVariable(variable, commParam, isCommunicated);
            }
            catch
            {
            }
        }

        return protocolVariable
            ?? throw new InvalidOperationException($"Protocol variable for \"{variable.Name}\" could not be created by \"{protocol.Specification.Name}\".");
    }

    public static string GetCommParam(IProtVariableSpecification specification)
    {
        var properties = specification.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead)
            .ToList();

        if (properties.FirstOrDefault(property => property.Name == "CommParams")?.GetValue(specification) is string commParams)
        {
            return commParams;
        }

        var parameters = new List<string>();
        AddCommParam(parameters, properties, specification, "Direction", value => value.ToString()!.ToLowerInvariant());
        AddCommParam(parameters, properties, specification, "VariableEvent", value => ((IVarEvent)value).Name, "eventRef");
        AddCommParam(parameters, properties, specification, "Multiplier");

        foreach (var property in properties.Where(property => property.Name is not ("Name" or "Direction" or "VariableEvent" or "Multiplier")))
        {
            var value = property.GetValue(specification);
            if (value == null)
            {
                continue;
            }

            parameters.Add($"{ToCamelCase(property.Name)}=\"{Convert.ToString(value, CultureInfo.InvariantCulture)}\"");
        }

        return string.Join(";", parameters);
    }

    public static string CreateDefaultCommParam(IVariableBase variable, IEnumerable<IVarEvent> variableEvents)
    {
        var eventName = variableEvents.FirstOrDefault(variableEvent => variableEvent is PeriodicVarEvent)?.Name
            ?? variableEvents.FirstOrDefault()?.Name;
        var parameters = new List<string>
        {
            "direction=\"read\""
        };

        if (!string.IsNullOrWhiteSpace(eventName))
        {
            parameters.Add($"eventRef=\"{eventName}\"");
        }

        parameters.Add("multiplier=\"1\"");
        parameters.Add($"id=\"{variable.Name}\"");
        return string.Join(";", parameters);
    }

    public static string CreateDefaultCommParam(IProtocolBase protocol, IVariableBase variable, IEnumerable<IVarEvent> variableEvents)
    {
        if (protocol.Specification.Name.Equals(PiZeroJsonProtocolName, StringComparison.OrdinalIgnoreCase))
        {
            return string.Join(";",
                "direction=\"read\"",
                "multiplier=\"1\"",
                $"id=\"{variable.Name}\"");
        }

        return CreateDefaultCommParam(variable, variableEvents);
    }

    public static Dictionary<string, string> ParseCommParam(string commParam)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parts in commParam
                     .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Select(parameter => parameter.Split('=', 2))
                     .Where(parts => parts.Length == 2))
        {
            parameters[parts[0].Trim()] = parts[1].Trim().Trim('"');
        }

        return parameters;
    }

    private static void AddCommParam(
        ICollection<string> parameters,
        IEnumerable<PropertyInfo> properties,
        object specification,
        string propertyName,
        Func<object, string>? valueFormatter = null,
        string? parameterName = null)
    {
        var property = properties.FirstOrDefault(property => property.Name == propertyName);
        var value = property?.GetValue(specification);
        if (value == null)
        {
            return;
        }

        var text = valueFormatter?.Invoke(value) ?? Convert.ToString(value, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        parameters.Add($"{parameterName ?? ToCamelCase(propertyName)}=\"{text}\"");
    }

    private static string ToCamelCase(string value)
    {
        return string.IsNullOrEmpty(value)
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];
    }
}
