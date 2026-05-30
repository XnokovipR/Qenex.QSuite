using System.Collections.ObjectModel;
using System.ComponentModel;
using Qenex.QInsight.EventAggregatorMsgs;
using Qenex.QInsight.ViewModels.ModelWrappers;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Scripting.PythonScript;
using Qenex.QSuite.Scripting.Script;
using Qenex.QSuite.Scripting.ScriptingEngine;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.Variables.ValueConversion;
using Qenex.QSuite.Variables.ValuePresentation;
using Qenex.QSuite.Variables.VariableEvents;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace Qenex.QInsight.ViewModels;

public class ProjectConfigurationViewModel : PropertyChangedBase
{
    private readonly EventAggregator? eventAggregator;
    private readonly IEnumerable<IDriverBase> drivers;
    private readonly IList<IVariableBase> variables;
    private readonly IList<IVarEvent> variableEvents;
    private readonly IList<IValConversion> conversions;
    private readonly IList<IPresentation> presentations;
    private readonly IList<IScriptBase> scripts;
    private readonly IList<OnValueChangedScriptTrigger> onValueChangedScriptTriggers;
    private readonly List<ProjectConfigurationVariableWrapper> addedVariables = [];
    private readonly List<ProjectConfigurationVariableWrapper> removedVariables = [];
    private readonly List<ProjectConfigurationProtocolVariableWrapper> addedCommunicatedVariables = [];
    private readonly List<RemovedCommunicatedVariable> removedCommunicatedVariables = [];
    private readonly List<ProjectConfigurationConversionWrapper> addedConversions = [];
    private readonly List<ProjectConfigurationConversionWrapper> removedConversions = [];
    private readonly List<ProjectConfigurationPresentationWrapper> addedPresentations = [];
    private readonly List<ProjectConfigurationPresentationWrapper> removedPresentations = [];
    private readonly List<ProjectConfigurationEventWrapper> addedEvents = [];
    private readonly List<ProjectConfigurationEventWrapper> removedEvents = [];
    private readonly List<ProjectConfigurationScriptWrapper> addedScripts = [];
    private readonly List<ProjectConfigurationScriptWrapper> removedScripts = [];
    private RadWindow? parentWindow;

    public ProjectConfigurationViewModel()
        : this(null, [], [], [], [], [], [], [])
    {
    }

    public ProjectConfigurationViewModel(
        EventAggregator? eventAggregator,
        IEnumerable<IDriverBase> drivers,
        IEnumerable<IVariableBase> variables,
        IEnumerable<IValConversion> conversions,
        IEnumerable<IPresentation> presentations,
        IEnumerable<IVarEvent> variableEvents,
        IList<OnValueChangedScriptTrigger> onValueChangedScriptTriggers,
        IList<IScriptBase> scripts)
    {
        this.eventAggregator = eventAggregator;
        this.drivers = drivers.ToList();
        this.variables = variables as IList<IVariableBase> ?? variables.ToList();
        this.variableEvents = variableEvents as IList<IVarEvent> ?? variableEvents.ToList();
        this.conversions = conversions as IList<IValConversion> ?? conversions.ToList();
        this.presentations = presentations as IList<IPresentation> ?? presentations.ToList();
        this.onValueChangedScriptTriggers = onValueChangedScriptTriggers;
        this.scripts = scripts;
        NavigationItems =
        [
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.CommunicationDrivers, "Communication Drivers"),
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.Events, "Events"),
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.Variables, "Variables"),
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.Presentations, "Presentations"),
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.Conversions, "Conversions"),
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.Scripts, "Scripts")
        ];
        Drivers = new ObservableCollection<ProjectConfigurationDriverWrapper>(
            drivers.Select(driver => new ProjectConfigurationDriverWrapper(driver)));
        foreach (var driver in Drivers)
        {
            driver.PropertyChanged += OnDriverPropertyChanged;
        }
        Variables = new ObservableCollection<ProjectConfigurationVariableWrapper>(
            this.variables.Select(variable => new ProjectConfigurationVariableWrapper(variable, presentations)));
        foreach (var variable in Variables)
        {
            variable.PropertyChanged += OnVariablePropertyChanged;
        }

        Conversions = new ObservableCollection<ProjectConfigurationConversionWrapper>(
            conversions.Select(conversion => new ProjectConfigurationConversionWrapper(conversion)));
        foreach (var conversion in Conversions)
        {
            conversion.PropertyChanged += OnConversionPropertyChanged;
        }

        Presentations = new ObservableCollection<ProjectConfigurationPresentationWrapper>(
            presentations.Select(presentation => CreatePresentationWrapper(presentation)));
        foreach (var presentation in Presentations)
        {
            presentation.PropertyChanged += OnPresentationPropertyChanged;
        }

        Events = new ObservableCollection<ProjectConfigurationEventWrapper>(
            this.variableEvents.Select(variableEvent => new ProjectConfigurationEventWrapper(variableEvent)));
        foreach (var variableEvent in Events)
        {
            variableEvent.PropertyChanged += OnEventPropertyChanged;
        }

        Scripts = new ObservableCollection<ProjectConfigurationScriptWrapper>(
            this.scripts.Select(script => new ProjectConfigurationScriptWrapper(script)));
        foreach (var script in Scripts)
        {
            script.PropertyChanged += OnScriptPropertyChanged;
        }

        SourceOptions = CreateSourceOptions(this.drivers).ToList();
        SelectedSourceOption = SourceOptions.FirstOrDefault();
        CommunicatedVariables = new ObservableCollection<ProjectConfigurationProtocolVariableWrapper>(
            SourceOptions.SelectMany(source => source.Protocol.Variables.Select(protocolVariable =>
                CreateCommunicatedVariableWrapper(protocolVariable, source))));
        foreach (var communicatedVariable in CommunicatedVariables)
        {
            communicatedVariable.PropertyChanged += OnCommunicatedVariablePropertyChanged;
        }

        ApplyCommand = new RelayCommand<object>(_ => ApplyChanges(), _ => HasChanges);
        CancelCommand = new RelayCommand<object>(_ => Cancel());
        AddVariableCommand = new RelayCommand<object>(_ => AddVariable(), _ => CanAddVariable());
        RemoveVariableCommand = new RelayCommand<object>(_ => RemoveVariable(), _ => SelectedVariable != null);
        AddVariableToSourceCommand = new RelayCommand<object>(_ => AddVariableToSource(), _ => CanAddVariableToSource());
        RemoveCommunicatedVariableCommand = new RelayCommand<object>(_ => RemoveCommunicatedVariable(), _ => SelectedCommunicatedVariable != null);
        SelectScriptCommand = new RelayCommand<object>(SelectScript);
        AddConversionCommand = new RelayCommand<object>(_ => AddConversion());
        RemoveConversionCommand = new RelayCommand<object>(_ => RemoveConversion(), _ => SelectedConversion != null);
        AddPresentationCommand = new RelayCommand<object>(_ => AddPresentation(), _ => CanAddPresentation());
        RemovePresentationCommand = new RelayCommand<object>(_ => RemovePresentation(), _ => CanRemovePresentation());
        AddEventCommand = new RelayCommand<object>(_ => AddEvent());
        RemoveEventCommand = new RelayCommand<object>(_ => RemoveEvent(), _ => CanRemoveEvent());
        AddScriptCommand = new RelayCommand<object>(_ => AddScript());
        RemoveScriptCommand = new RelayCommand<object>(_ => RemoveScript(), _ => SelectedScript != null);
        EnsureInitialVariableSelections();
        SelectedConversion = Conversions.FirstOrDefault();
        SelectedPresentation = Presentations.FirstOrDefault();
        SelectedEvent = Events.FirstOrDefault();
        SelectedScript = Scripts.FirstOrDefault();
        SelectedNavigationItem = NavigationItems[0];
    }

    public ObservableCollection<ProjectConfigurationNavigationItem> NavigationItems { get; }
    public ObservableCollection<ProjectConfigurationDriverWrapper> Drivers { get; }
    public ObservableCollection<ProjectConfigurationVariableWrapper> Variables { get; }
    public ObservableCollection<ProjectConfigurationConversionWrapper> Conversions { get; }
    public ObservableCollection<ProjectConfigurationPresentationWrapper> Presentations { get; }
    public ObservableCollection<ProjectConfigurationEventWrapper> Events { get; }
    public ObservableCollection<ProjectConfigurationProtocolVariableWrapper> CommunicatedVariables { get; }
    public IReadOnlyList<ProjectConfigurationProtocolOption> SourceOptions { get; }
    public ObservableCollection<ProjectConfigurationScriptWrapper> Scripts { get; }
    public IEnumerable<EnumMemberViewModel> ExecutionModes { get; } = EnumDataSource.FromType<ScriptExecutionMode>();
    public RelayCommand<object> ApplyCommand { get; }
    public RelayCommand<object> CancelCommand { get; }
    public RelayCommand<object> AddVariableCommand { get; }
    public RelayCommand<object> RemoveVariableCommand { get; }
    public RelayCommand<object> AddVariableToSourceCommand { get; }
    public RelayCommand<object> RemoveCommunicatedVariableCommand { get; }
    public RelayCommand<object> SelectScriptCommand { get; }
    public RelayCommand<object> AddConversionCommand { get; }
    public RelayCommand<object> RemoveConversionCommand { get; }
    public RelayCommand<object> AddPresentationCommand { get; }
    public RelayCommand<object> RemovePresentationCommand { get; }
    public RelayCommand<object> AddEventCommand { get; }
    public RelayCommand<object> RemoveEventCommand { get; }
    public RelayCommand<object> AddScriptCommand { get; }
    public RelayCommand<object> RemoveScriptCommand { get; }

    public ProjectConfigurationVariableWrapper? SelectedVariable
    {
        get => field;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            RemoveVariableCommand?.OnCanExecuteChanged();
            AddVariableToSourceCommand?.OnCanExecuteChanged();
        }
    }

    public ProjectConfigurationProtocolVariableWrapper? SelectedCommunicatedVariable
    {
        get => field;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            RemoveCommunicatedVariableCommand?.OnCanExecuteChanged();
        }
    }

    public ProjectConfigurationScriptWrapper? SelectedScript
    {
        get => field;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            RemoveScriptCommand?.OnCanExecuteChanged();
        }
    }

    public ProjectConfigurationConversionWrapper? SelectedConversion
    {
        get => field;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            RemoveConversionCommand?.OnCanExecuteChanged();
        }
    }

    public ProjectConfigurationPresentationWrapper? SelectedPresentation
    {
        get => field;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            ErrorMessage = string.Empty;
            OnPropertyChanged();
            RemovePresentationCommand?.OnCanExecuteChanged();
        }
    }

    public ProjectConfigurationEventWrapper? SelectedEvent
    {
        get => field;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            ErrorMessage = string.Empty;
            OnPropertyChanged();
            RemoveEventCommand?.OnCanExecuteChanged();
        }
    }

    public ProjectConfigurationProtocolOption? SelectedSourceOption
    {
        get => field;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            AddVariableToSourceCommand?.OnCanExecuteChanged();
        }
    }

    public string ErrorMessage
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    } = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasChanges =>
        Drivers.Any(driver => driver.HasChanges)
        || Variables.Any(variable => variable.HasChanges)
        || addedVariables.Count > 0
        || removedVariables.Count > 0
        || Conversions.Any(conversion => conversion.HasChanges)
        || addedConversions.Count > 0
        || removedConversions.Count > 0
        || Presentations.Any(presentation => presentation.HasChanges)
        || addedPresentations.Count > 0
        || removedPresentations.Count > 0
        || Events.Any(variableEvent => variableEvent.HasChanges)
        || addedEvents.Count > 0
        || removedEvents.Count > 0
        || CommunicatedVariables.Any(variable => variable.HasChanges)
        || addedCommunicatedVariables.Count > 0
        || removedCommunicatedVariables.Count > 0
        || addedScripts.Count > 0
        || removedScripts.Count > 0
        || Scripts.Any(script => script.HasChanges);

    public ProjectConfigurationNavigationItem SelectedNavigationItem
    {
        get => field;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedSection));
            if (field.Section == ProjectConfigurationSection.Variables)
            {
                EnsureInitialVariableSelections();
            }
        }
    }

    public ProjectConfigurationSection SelectedSection => SelectedNavigationItem.Section;

    public void SelectSection(ProjectConfigurationSection section)
    {
        SelectedNavigationItem = NavigationItems.First(item => item.Section == section);
    }

    private void EnsureInitialVariableSelections()
    {
        SelectedVariable ??= Variables.FirstOrDefault();
        SelectedSourceOption ??= SourceOptions.FirstOrDefault();
        SelectedCommunicatedVariable ??= SelectedSourceOption == null
            ? CommunicatedVariables.FirstOrDefault()
            : CommunicatedVariables.FirstOrDefault(variable => variable.SelectedSource == SelectedSourceOption)
              ?? CommunicatedVariables.FirstOrDefault();
    }

    public void SetParentWindow(RadWindow window)
    {
        parentWindow = window;
    }

    private void ApplyChanges()
    {
        var changedVariables = Variables
            .Where(variable => variable.HasChanges)
            .Select(variable => variable.Variable)
            .ToList();

        foreach (var driver in Drivers)
        {
            driver.ApplyChanges();
        }
        foreach (var variable in Variables)
        {
            variable.ApplyChanges();
        }
        foreach (var removedVariable in removedVariables)
        {
            variables.Remove(removedVariable.Variable);
        }
        foreach (var addedVariable in addedVariables)
        {
            if (!variables.Contains(addedVariable.Variable))
            {
                variables.Add(addedVariable.Variable);
            }
        }
        addedVariables.Clear();
        removedVariables.Clear();
        foreach (var conversion in Conversions)
        {
            conversion.ApplyChanges();
        }
        foreach (var removedConversion in removedConversions)
        {
            conversions.Remove(removedConversion.Conversion);
        }
        foreach (var addedConversion in addedConversions)
        {
            var conversion = addedConversion.ApplyChanges();
            if (!conversions.Contains(conversion))
            {
                conversions.Add(conversion);
            }
        }
        addedConversions.Clear();
        removedConversions.Clear();
        foreach (var presentation in Presentations)
        {
            presentation.ApplyChanges();
        }
        foreach (var removedPresentation in removedPresentations)
        {
            presentations.Remove(removedPresentation.Presentation);
        }
        foreach (var addedPresentation in addedPresentations)
        {
            if (!presentations.Contains(addedPresentation.Presentation))
            {
                presentations.Add(addedPresentation.Presentation);
            }
        }
        addedPresentations.Clear();
        removedPresentations.Clear();
        SynchronizeRenamedEventReferences();
        foreach (var variableEvent in Events)
        {
            variableEvent.ApplyChanges();
        }
        foreach (var removedEvent in removedEvents)
        {
            variableEvents.Remove(removedEvent.VariableEvent);
        }
        foreach (var addedEvent in addedEvents)
        {
            var variableEvent = addedEvent.ApplyChanges();
            if (!variableEvents.Contains(variableEvent))
            {
                variableEvents.Add(variableEvent);
            }
        }
        addedEvents.Clear();
        removedEvents.Clear();
        foreach (var removed in removedCommunicatedVariables)
        {
            if (CommunicatedVariables.Any(variable => variable.Variable.Id == removed.ProtocolVariable.Variable.Id))
            {
                continue;
            }

            foreach (var trigger in onValueChangedScriptTriggers
                         .Where(trigger => trigger.VariableId == removed.ProtocolVariable.Variable.Id)
                         .ToList())
            {
                onValueChangedScriptTriggers.Remove(trigger);
            }
        }

        foreach (var communicatedVariable in CommunicatedVariables)
        {
            communicatedVariable.ApplyChanges();
            communicatedVariable.ApplyScriptTrigger(onValueChangedScriptTriggers);
        }

        SynchronizeFileLogAndReplayVariables();
        addedCommunicatedVariables.Clear();
        removedCommunicatedVariables.Clear();

        var removedScriptModels = removedScripts
            .Select(script => script.Script)
            .ToList();

        foreach (var script in Scripts)
        {
            script.ApplyChanges();
        }
        foreach (var removedScript in removedScripts)
        {
            RemoveScriptTriggers(removedScript);
            scripts.Remove(removedScript.Script);
        }
        foreach (var addedScript in addedScripts)
        {
            if (!scripts.Any(script => string.Equals(script.FileName, addedScript.FileName, StringComparison.OrdinalIgnoreCase)))
            {
                scripts.Add(addedScript.Script);
            }
        }
        addedScripts.Clear();
        removedScripts.Clear();

        foreach (var variable in changedVariables)
        {
            eventAggregator?.Publish(new VariablePropertiesChangedMsg { Variable = variable });
        }
        if (removedScriptModels.Count > 0)
        {
            eventAggregator?.Publish(new ScriptsRemovedMsg { Scripts = removedScriptModels });
        }

        eventAggregator?.Publish(new ProjectConfigurationAppliedMsg());
        NotifyHasChangesChanged();
    }

    private void Cancel()
    {
        foreach (var driver in Drivers)
        {
            driver.CancelChanges();
        }
        foreach (var variable in Variables)
        {
            variable.CancelChanges();
        }
        addedVariables.Clear();
        removedVariables.Clear();
        foreach (var conversion in Conversions)
        {
            conversion.CancelChanges();
        }
        addedConversions.Clear();
        removedConversions.Clear();
        foreach (var presentation in Presentations)
        {
            presentation.CancelChanges();
        }
        addedPresentations.Clear();
        removedPresentations.Clear();
        foreach (var variableEvent in Events)
        {
            variableEvent.CancelChanges();
        }
        addedEvents.Clear();
        removedEvents.Clear();
        foreach (var communicatedVariable in CommunicatedVariables)
        {
            communicatedVariable.CancelChanges();
        }
        foreach (var communicatedVariable in addedCommunicatedVariables)
        {
            communicatedVariable.SelectedSource.Protocol.RemoveProtocolVariable(communicatedVariable.ProtocolVariable);
        }
        foreach (var removed in removedCommunicatedVariables)
        {
            removed.Source.Protocol.AddVariable(removed.ProtocolVariable);
        }

        foreach (var script in Scripts)
        {
            script.CancelChanges();
        }
        addedScripts.Clear();
        removedScripts.Clear();

        parentWindow?.Close();
    }

    private void AddVariable()
    {
        if (!CanAddVariable())
        {
            return;
        }

        var values = ValuesGlobal.CreateInstance(ValuesGlobal.ValueDataType.Int);
        values.ValPresentation = Presentations.First().Presentation;
        var variable = new ScalarVariable
        {
            Id = CreateUniqueVariableId(),
            Namespace = "/",
            Name = CreateUniqueVariableName(),
            Label = "Variable",
            Description = string.Empty,
            Size = values.Size,
            Values = values
        };
        var wrapper = new ProjectConfigurationVariableWrapper(
            variable,
            Presentations.Select(presentation => presentation.Presentation),
            isNew: true);
        wrapper.PropertyChanged += OnVariablePropertyChanged;
        Variables.Add(wrapper);
        addedVariables.Add(wrapper);
        SelectedVariable = wrapper;

        NotifyHasChangesChanged();
    }

    private bool CanAddVariable()
    {
        return Presentations.Count > 0;
    }

    private void RemoveVariable()
    {
        if (SelectedVariable == null)
        {
            return;
        }

        var variable = SelectedVariable;
        foreach (var communicatedVariable in CommunicatedVariables
                     .Where(communicatedVariable => communicatedVariable.Variable.Id == variable.Id)
                     .ToList())
        {
            RemoveCommunicatedVariable(communicatedVariable);
        }

        variable.PropertyChanged -= OnVariablePropertyChanged;
        Variables.Remove(variable);

        if ((!variable.IsNew || !addedVariables.Remove(variable)) && !removedVariables.Contains(variable))
        {
            removedVariables.Add(variable);
        }

        SelectedVariable = Variables.FirstOrDefault();
        NotifyHasChangesChanged();
    }

    private int CreateUniqueVariableId()
    {
        var usedIds = Variables
            .Select(variable => variable.Id)
            .Concat(variables.Select(variable => variable.Id))
            .ToHashSet();

        var id = usedIds.Count == 0 ? 1 : usedIds.Max() + 1;
        while (usedIds.Contains(id))
        {
            id++;
        }

        return id;
    }

    private string CreateUniqueVariableName()
    {
        const string baseName = "variable";

        var existingNames = Variables
            .Select(variable => variable.Variable.Name)
            .Concat(variables.Select(variable => variable.Name))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var name = baseName;
        var index = 1;
        while (existingNames.Contains(name))
        {
            name = $"{baseName}_{index}";
            index++;
        }

        return name;
    }

    private void AddConversion()
    {
        var conversion = ProjectConfigurationConversionWrapper.CreateConversion(
            CreateUniqueConversionName(),
            ConversionsGlobal.ConversionType.Linear);
        var wrapper = new ProjectConfigurationConversionWrapper(conversion, isNew: true);
        wrapper.PropertyChanged += OnConversionPropertyChanged;
        Conversions.Add(wrapper);
        addedConversions.Add(wrapper);
        SelectedConversion = wrapper;

        NotifyHasChangesChanged();
    }

    private void RemoveConversion()
    {
        if (SelectedConversion == null)
        {
            return;
        }

        var conversion = SelectedConversion;
        conversion.PropertyChanged -= OnConversionPropertyChanged;
        Conversions.Remove(conversion);

        if (conversion.IsNew)
        {
            addedConversions.Remove(conversion);
        }
        else if (!removedConversions.Contains(conversion))
        {
            removedConversions.Add(conversion);
        }

        SelectedConversion = Conversions.FirstOrDefault();
        NotifyHasChangesChanged();
    }

    private string CreateUniqueConversionName()
    {
        const string baseName = "conversion";

        var existingNames = Conversions
            .Select(conversion => conversion.Name)
            .Concat(conversions.Select(conversion => conversion.Name))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var name = baseName;
        var index = 1;
        while (existingNames.Contains(name))
        {
            name = $"{baseName}_{index}";
            index++;
        }

        return name;
    }

    private ProjectConfigurationPresentationWrapper CreatePresentationWrapper(IPresentation presentation, bool isNew = false)
    {
        return new ProjectConfigurationPresentationWrapper(
            presentation,
            Conversions.Select(conversion => conversion.Conversion),
            isNew);
    }

    private void AddPresentation()
    {
        if (!CanAddPresentation())
        {
            return;
        }

        var conversion = Conversions.First().Conversion;
        var presentation = new Presentation
        {
            Name = CreateUniquePresentationName(),
            Label = "Presentation",
            Min = 0,
            Max = 1,
            PrintFormat = "{0}",
            Unit = string.Empty,
            Conversion = conversion
        };
        var wrapper = CreatePresentationWrapper(presentation, isNew: true);
        wrapper.PropertyChanged += OnPresentationPropertyChanged;
        Presentations.Add(wrapper);
        addedPresentations.Add(wrapper);
        SelectedPresentation = wrapper;

        NotifyHasChangesChanged();
    }

    private bool CanAddPresentation()
    {
        return Conversions.Count > 0;
    }

    private void RemovePresentation()
    {
        if (SelectedPresentation == null)
        {
            return;
        }

        if (IsPresentationUsed(SelectedPresentation))
        {
            ErrorMessage = $"Presentation \"{SelectedPresentation.Name}\" is used by a variable.";
            return;
        }

        ErrorMessage = string.Empty;
        var presentation = SelectedPresentation;
        presentation.PropertyChanged -= OnPresentationPropertyChanged;
        Presentations.Remove(presentation);

        if (presentation.IsNew)
        {
            addedPresentations.Remove(presentation);
        }
        else if (!removedPresentations.Contains(presentation))
        {
            removedPresentations.Add(presentation);
        }

        SelectedPresentation = Presentations.FirstOrDefault();
        NotifyHasChangesChanged();
    }

    private bool CanRemovePresentation()
    {
        return SelectedPresentation != null && !IsPresentationUsed(SelectedPresentation);
    }

    private bool IsPresentationUsed(ProjectConfigurationPresentationWrapper presentation)
    {
        return Variables.Any(variable =>
            variable.Variable is ScalarVariable scalarVariable
            && ReferenceEquals(scalarVariable.Values.ValPresentation, presentation.Presentation));
    }

    private string CreateUniquePresentationName()
    {
        const string baseName = "presentation";

        var existingNames = Presentations
            .Select(presentation => presentation.Name)
            .Concat(presentations.Select(presentation => presentation.Name))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var name = baseName;
        var index = 1;
        while (existingNames.Contains(name))
        {
            name = $"{baseName}_{index}";
            index++;
        }

        return name;
    }

    private void AddEvent()
    {
        var variableEvent = ProjectConfigurationEventWrapper.CreateEvent(
            CreateUniqueEventName(),
            EventsGlobal.VariableEventType.Periodic);
        var wrapper = new ProjectConfigurationEventWrapper(variableEvent, isNew: true);
        wrapper.PropertyChanged += OnEventPropertyChanged;
        Events.Add(wrapper);
        addedEvents.Add(wrapper);
        SelectedEvent = wrapper;

        RefreshCommunicatedVariableEventOptions();
        NotifyHasChangesChanged();
    }

    private void RemoveEvent()
    {
        if (SelectedEvent == null)
        {
            return;
        }

        if (IsEventUsed(SelectedEvent))
        {
            ErrorMessage = $"Event \"{SelectedEvent.Name}\" is used by a communicated variable.";
            return;
        }

        ErrorMessage = string.Empty;
        var variableEvent = SelectedEvent;
        variableEvent.PropertyChanged -= OnEventPropertyChanged;
        Events.Remove(variableEvent);

        if (variableEvent.IsNew)
        {
            addedEvents.Remove(variableEvent);
        }
        else if (!removedEvents.Contains(variableEvent))
        {
            removedEvents.Add(variableEvent);
        }

        SelectedEvent = Events.FirstOrDefault();
        RefreshCommunicatedVariableEventOptions();
        NotifyHasChangesChanged();
    }

    private bool CanRemoveEvent()
    {
        return SelectedEvent != null && !IsEventUsed(SelectedEvent);
    }

    private bool IsEventUsed(ProjectConfigurationEventWrapper variableEvent)
    {
        var eventNames = new[] { variableEvent.Name, variableEvent.OriginalName, variableEvent.VariableEvent.Name }
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return CommunicatedVariables.Any(variable => eventNames.Contains(variable.SelectedVariableEventName));
    }

    private void SynchronizeRenamedEventReferences()
    {
        foreach (var variableEvent in Events.Where(variableEvent =>
                     !string.Equals(variableEvent.OriginalName, variableEvent.Name, StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var communicatedVariable in CommunicatedVariables.Where(communicatedVariable =>
                         string.Equals(communicatedVariable.SelectedVariableEventName, variableEvent.OriginalName, StringComparison.OrdinalIgnoreCase)))
            {
                communicatedVariable.SelectedVariableEventName = variableEvent.Name;
            }
        }
    }

    private string CreateUniqueEventName()
    {
        const string baseName = "event";

        var existingNames = Events
            .Select(variableEvent => variableEvent.Name)
            .Concat(variableEvents.Select(variableEvent => variableEvent.Name))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var name = baseName;
        var index = 1;
        while (existingNames.Contains(name))
        {
            name = $"{baseName}_{index}";
            index++;
        }

        return name;
    }

    private void AddScript()
    {
        var script = new PyScript
        {
            FileName = CreateUniqueScriptFileName(),
            IsEnabled = true,
            IsReplayEnabled = false,
            ExecutionMode = ScriptExecutionMode.Manual,
            Blocking = true
        };
        var wrapper = new ProjectConfigurationScriptWrapper(script, isNew: true);
        wrapper.PropertyChanged += OnScriptPropertyChanged;
        Scripts.Add(wrapper);
        addedScripts.Add(wrapper);
        SelectedScript = wrapper;

        RefreshCommunicatedVariableScriptOptions();
        NotifyHasChangesChanged();
    }

    private void SelectScript(object? parameter)
    {
        if (parameter is ProjectConfigurationScriptWrapper script)
        {
            SelectedScript = script;
        }
    }

    private void RemoveScript()
    {
        if (SelectedScript == null)
        {
            return;
        }

        var script = SelectedScript;
        script.PropertyChanged -= OnScriptPropertyChanged;
        Scripts.Remove(script);

        if (script.IsNew)
        {
            addedScripts.Remove(script);
        }
        else if (!removedScripts.Contains(script))
        {
            removedScripts.Add(script);
        }

        ClearRemovedScriptReferences(script);
        SelectedScript = Scripts.FirstOrDefault();

        RefreshCommunicatedVariableScriptOptions();
        NotifyHasChangesChanged();
    }

    private string CreateUniqueScriptFileName()
    {
        const string baseName = "script";
        const string extension = ".py";

        var existingNames = Scripts
            .Select(script => script.FileName)
            .Concat(scripts.Select(script => script.FileName))
            .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var fileName = $"{baseName}{extension}";
        var index = 1;
        while (existingNames.Contains(fileName))
        {
            fileName = $"{baseName}_{index}{extension}";
            index++;
        }

        return fileName;
    }

    private void ClearRemovedScriptReferences(ProjectConfigurationScriptWrapper removedScript)
    {
        foreach (var communicatedVariable in CommunicatedVariables)
        {
            if (IsRemovedScriptReference(communicatedVariable.SelectedScriptFileName, removedScript))
            {
                communicatedVariable.SelectedScriptFileName = string.Empty;
                communicatedVariable.ScriptAdditionalInfo = string.Empty;
            }
        }
    }

    private static bool IsRemovedScriptReference(string scriptFileName, ProjectConfigurationScriptWrapper removedScript)
    {
        return string.Equals(scriptFileName, removedScript.FileName, StringComparison.OrdinalIgnoreCase)
               || string.Equals(scriptFileName, removedScript.OriginalFileName, StringComparison.OrdinalIgnoreCase);
    }

    private void RemoveScriptTriggers(ProjectConfigurationScriptWrapper removedScript)
    {
        foreach (var trigger in onValueChangedScriptTriggers
                     .Where(trigger => IsRemovedScriptReference(trigger.ScriptFileName, removedScript))
                     .ToList())
        {
            onValueChangedScriptTriggers.Remove(trigger);
        }
    }

    private void AddVariableToSource()
    {
        if (SelectedVariable == null || SelectedSourceOption == null)
        {
            return;
        }

        try
        {
            ErrorMessage = string.Empty;
            if (SelectedSourceOption.Protocol.Variables.Any(variable => variable.Variable.Id == SelectedVariable.Variable.Id))
            {
                ErrorMessage = "Variable is already assigned to the selected source protocol.";
                return;
            }

            var protocolVariable = ProjectConfigurationProtocolVariableFactory.CreateProtocolVariable(
                SelectedSourceOption.Protocol,
                SelectedVariable.Variable,
                GetConfigurationVariableEvents(),
                ProjectConfigurationProtocolVariableFactory.CreateDefaultCommParam(SelectedVariable.Variable, GetConfigurationVariableEvents()),
                true);
            SelectedSourceOption.Protocol.AddVariable(protocolVariable);

            var wrapper = CreateCommunicatedVariableWrapper(protocolVariable, SelectedSourceOption);
            wrapper.PropertyChanged += OnCommunicatedVariablePropertyChanged;
            CommunicatedVariables.Add(wrapper);
            addedCommunicatedVariables.Add(wrapper);
            SelectedCommunicatedVariable = wrapper;
            NotifyHasChangesChanged();
        }
        catch (Exception e)
        {
            ErrorMessage = e.Message;
        }
    }

    private bool CanAddVariableToSource()
    {
        return SelectedVariable != null && SelectedSourceOption != null;
    }

    private void RemoveCommunicatedVariable()
    {
        if (SelectedCommunicatedVariable == null)
        {
            return;
        }

        RemoveCommunicatedVariable(SelectedCommunicatedVariable);
    }

    private void RemoveCommunicatedVariable(ProjectConfigurationProtocolVariableWrapper communicatedVariable)
    {
        communicatedVariable.PropertyChanged -= OnCommunicatedVariablePropertyChanged;
        CommunicatedVariables.Remove(communicatedVariable);
        communicatedVariable.SelectedSource.Protocol.RemoveProtocolVariable(communicatedVariable.ProtocolVariable);

        if (!addedCommunicatedVariables.Remove(communicatedVariable))
        {
            removedCommunicatedVariables.Add(new RemovedCommunicatedVariable(
                communicatedVariable.ProtocolVariable,
                communicatedVariable.SelectedSource));
        }

        SelectedCommunicatedVariable = CommunicatedVariables.FirstOrDefault();
        NotifyHasChangesChanged();
    }

    private ProjectConfigurationProtocolVariableWrapper CreateCommunicatedVariableWrapper(
        IProtocolVariable protocolVariable,
        ProjectConfigurationProtocolOption source)
    {
        return new ProjectConfigurationProtocolVariableWrapper(
            protocolVariable,
            source,
            SourceOptions,
            GetConfigurationVariableEvents(),
            Scripts,
            onValueChangedScriptTriggers.FirstOrDefault(trigger => trigger.VariableId == protocolVariable.Variable.Id),
            IsFileLogEnabled(protocolVariable.Variable),
            () => Events.Select(variableEvent => variableEvent.Name));
    }

    private IEnumerable<IVarEvent> GetConfigurationVariableEvents()
    {
        return Events.Select(variableEvent => variableEvent.VariableEvent);
    }

    private void SynchronizeFileLogAndReplayVariables()
    {
        var loggedVariables = CommunicatedVariables
            .Where(variable => variable.IsCommunicated && variable.IsFileLogEnabled)
            .Select(variable => variable.Variable)
            .DistinctBy(variable => variable.Id)
            .ToList();

        var fileLogDriver = drivers.FirstOrDefault(IsFileLogDriver);
        if (fileLogDriver != null)
        {
            SynchronizeSinkDriverVariables(fileLogDriver, loggedVariables);
        }

        var replayDriver = drivers.FirstOrDefault(IsReplayDriver);
        if (replayDriver != null)
        {
            SynchronizeReplayDriverVariables(replayDriver, loggedVariables);
        }
    }

    private void SynchronizeSinkDriverVariables(IDriverBase sinkDriver, IReadOnlyCollection<IVariableBase> loggedVariables)
    {
        var loggedIds = loggedVariables.Select(variable => variable.Id).ToHashSet();
        foreach (var protocol in sinkDriver.Protocols.Where(protocol => protocol is IProtocolVariableSinkProtocol))
        {
            foreach (var protocolVariable in protocol.Variables.Where(variable => !loggedIds.Contains(variable.Variable.Id)).ToList())
            {
                protocol.RemoveProtocolVariable(protocolVariable);
            }

            foreach (var variable in loggedVariables.Where(variable => protocol.Variables.All(protocolVariable => protocolVariable.Variable.Id != variable.Id)))
            {
                var protocolVariable = ProjectConfigurationProtocolVariableFactory.CreateProtocolVariable(
                    protocol,
                    variable,
                    GetConfigurationVariableEvents(),
                    string.Empty,
                    true);
                protocol.AddVariable(protocolVariable);
            }
        }
    }

    private void SynchronizeReplayDriverVariables(IDriverBase replayDriver, IReadOnlyCollection<IVariableBase> loggedVariables)
    {
        var loggedIds = loggedVariables.Select(variable => variable.Id).ToHashSet();
        foreach (var protocol in replayDriver.Protocols)
        {
            foreach (var protocolVariable in protocol.Variables.Where(variable => !loggedIds.Contains(variable.Variable.Id)).ToList())
            {
                protocol.RemoveProtocolVariable(protocolVariable);
            }

            foreach (var variable in loggedVariables.Where(variable => protocol.Variables.All(protocolVariable => protocolVariable.Variable.Id != variable.Id)))
            {
                var protocolVariable = ProjectConfigurationProtocolVariableFactory.CreateProtocolVariable(
                    protocol,
                    variable,
                    GetConfigurationVariableEvents(),
                    string.Empty,
                    true);
                protocol.AddVariable(protocolVariable);
            }
        }
    }

    private bool IsFileLogEnabled(IVariableBase variable)
    {
        return drivers
            .Where(IsFileLogDriver)
            .SelectMany(driver => driver.Protocols)
            .SelectMany(protocol => protocol.Variables)
            .Any(protocolVariable => protocolVariable.Variable.Id == variable.Id);
    }

    private static IEnumerable<ProjectConfigurationProtocolOption> CreateSourceOptions(IEnumerable<IDriverBase> drivers)
    {
        return drivers
            .Where(IsSourceDriver)
            .SelectMany(driver => driver.Protocols.Select(protocol => new ProjectConfigurationProtocolOption(driver, protocol)));
    }

    private static bool IsSourceDriver(IDriverBase driver)
    {
        return driver is not IProtocolVariableSinkDriver
               && driver is not IReplayDriver
               && !IsFileLogDriver(driver)
               && !IsReplayDriver(driver);
    }

    private static bool IsFileLogDriver(IDriverBase driver)
    {
        return driver.Specification.Name.Equals("FileDataLoggerDriver", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReplayDriver(IDriverBase driver)
    {
        return driver.Specification.Name.Equals("FileDataReplayDriver", StringComparison.OrdinalIgnoreCase);
    }

    private void OnDriverPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ProjectConfigurationDriverWrapper.HasChanges)
            && e.PropertyName != nameof(ProjectConfigurationDriverWrapper.IsEnabled))
        {
            return;
        }

        NotifyHasChangesChanged();
    }

    private void OnScriptPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ProjectConfigurationScriptWrapper.HasChanges))
        {
            return;
        }

        RefreshCommunicatedVariableScriptOptions();

        NotifyHasChangesChanged();
    }

    private void RefreshCommunicatedVariableScriptOptions()
    {
        foreach (var communicatedVariable in CommunicatedVariables)
        {
            communicatedVariable.RefreshScriptOptions();
        }
    }

    private void RefreshCommunicatedVariableEventOptions()
    {
        foreach (var communicatedVariable in CommunicatedVariables)
        {
            communicatedVariable.RefreshVariableEventOptions();
        }
    }

    private void OnVariablePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ProjectConfigurationVariableWrapper.HasChanges))
        {
            return;
        }

        NotifyHasChangesChanged();
    }

    private void OnConversionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ProjectConfigurationConversionWrapper.HasChanges))
        {
            return;
        }

        NotifyHasChangesChanged();
    }

    private void OnPresentationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ProjectConfigurationPresentationWrapper.HasChanges))
        {
            return;
        }

        NotifyHasChangesChanged();
    }

    private void OnEventPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ProjectConfigurationEventWrapper.HasChanges)
            && e.PropertyName != nameof(ProjectConfigurationEventWrapper.Name))
        {
            return;
        }

        RefreshCommunicatedVariableEventOptions();
        NotifyHasChangesChanged();
    }

    private void OnCommunicatedVariablePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ProjectConfigurationProtocolVariableWrapper.HasChanges))
        {
            return;
        }

        NotifyHasChangesChanged();
    }

    private void NotifyHasChangesChanged()
    {
        OnPropertyChanged(nameof(HasChanges));
        ApplyCommand.OnCanExecuteChanged();
        AddVariableCommand.OnCanExecuteChanged();
        RemoveVariableCommand.OnCanExecuteChanged();
        RemoveConversionCommand.OnCanExecuteChanged();
        AddPresentationCommand.OnCanExecuteChanged();
        RemovePresentationCommand.OnCanExecuteChanged();
        RemoveEventCommand.OnCanExecuteChanged();
        RemoveScriptCommand.OnCanExecuteChanged();
    }

    private sealed record RemovedCommunicatedVariable(
        IProtocolVariable ProtocolVariable,
        ProjectConfigurationProtocolOption Source);
}

public sealed record ProjectConfigurationNavigationItem(ProjectConfigurationSection Section, string Label);

public enum ProjectConfigurationSection
{
    CommunicationDrivers,
    Variables,
    Conversions,
    Presentations,
    Events,
    Scripts
}
