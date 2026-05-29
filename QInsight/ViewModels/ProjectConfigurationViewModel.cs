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
using Qenex.QSuite.Variables.ValuePresentation;
using Qenex.QSuite.Variables.VariableEvents;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace Qenex.QInsight.ViewModels;

public class ProjectConfigurationViewModel : PropertyChangedBase
{
    private readonly EventAggregator? eventAggregator;
    private readonly IEnumerable<IDriverBase> drivers;
    private readonly IEnumerable<IVarEvent> variableEvents;
    private readonly IList<IScriptBase> scripts;
    private readonly IList<OnValueChangedScriptTrigger> onValueChangedScriptTriggers;
    private readonly List<ProjectConfigurationProtocolVariableWrapper> addedCommunicatedVariables = [];
    private readonly List<RemovedCommunicatedVariable> removedCommunicatedVariables = [];
    private readonly List<ProjectConfigurationScriptWrapper> addedScripts = [];
    private readonly List<ProjectConfigurationScriptWrapper> removedScripts = [];
    private RadWindow? parentWindow;

    public ProjectConfigurationViewModel()
        : this(null, [], [], [], [], [], [])
    {
    }

    public ProjectConfigurationViewModel(
        EventAggregator? eventAggregator,
        IEnumerable<IDriverBase> drivers,
        IEnumerable<IVariableBase> variables,
        IEnumerable<IPresentation> presentations,
        IEnumerable<IVarEvent> variableEvents,
        IList<OnValueChangedScriptTrigger> onValueChangedScriptTriggers,
        IList<IScriptBase> scripts)
    {
        this.eventAggregator = eventAggregator;
        this.drivers = drivers.ToList();
        this.variableEvents = variableEvents.ToList();
        this.onValueChangedScriptTriggers = onValueChangedScriptTriggers;
        this.scripts = scripts;
        NavigationItems =
        [
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.CommunicationDrivers, "Communication Drivers"),
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.Variables, "Variables"),
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.Presentations, "Presentations"),
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.Events, "Events"),
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.Scripts, "Scripts")
        ];
        Drivers = new ObservableCollection<ProjectConfigurationDriverWrapper>(
            drivers.Select(driver => new ProjectConfigurationDriverWrapper(driver)));
        foreach (var driver in Drivers)
        {
            driver.PropertyChanged += OnDriverPropertyChanged;
        }
        Variables = new ObservableCollection<ProjectConfigurationVariableWrapper>(
            variables.Select(variable => new ProjectConfigurationVariableWrapper(variable, presentations)));
        foreach (var variable in Variables)
        {
            variable.PropertyChanged += OnVariablePropertyChanged;
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
        AddVariableToSourceCommand = new RelayCommand<object>(_ => AddVariableToSource(), _ => CanAddVariableToSource());
        RemoveCommunicatedVariableCommand = new RelayCommand<object>(_ => RemoveCommunicatedVariable(), _ => SelectedCommunicatedVariable != null);
        SelectScriptCommand = new RelayCommand<object>(SelectScript);
        AddScriptCommand = new RelayCommand<object>(_ => AddScript());
        RemoveScriptCommand = new RelayCommand<object>(_ => RemoveScript(), _ => SelectedScript != null);
        EnsureInitialVariableSelections();
        SelectedScript = Scripts.FirstOrDefault();
        SelectedNavigationItem = NavigationItems[0];
    }

    public ObservableCollection<ProjectConfigurationNavigationItem> NavigationItems { get; }
    public ObservableCollection<ProjectConfigurationDriverWrapper> Drivers { get; }
    public ObservableCollection<ProjectConfigurationVariableWrapper> Variables { get; }
    public ObservableCollection<ProjectConfigurationProtocolVariableWrapper> CommunicatedVariables { get; }
    public IReadOnlyList<ProjectConfigurationProtocolOption> SourceOptions { get; }
    public ObservableCollection<ProjectConfigurationScriptWrapper> Scripts { get; }
    public IEnumerable<EnumMemberViewModel> ExecutionModes { get; } = EnumDataSource.FromType<ScriptExecutionMode>();
    public RelayCommand<object> ApplyCommand { get; }
    public RelayCommand<object> CancelCommand { get; }
    public RelayCommand<object> AddVariableToSourceCommand { get; }
    public RelayCommand<object> RemoveCommunicatedVariableCommand { get; }
    public RelayCommand<object> SelectScriptCommand { get; }
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
                variableEvents,
                ProjectConfigurationProtocolVariableFactory.CreateDefaultCommParam(SelectedVariable.Variable, variableEvents),
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

        var communicatedVariable = SelectedCommunicatedVariable;
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
            variableEvents,
            Scripts,
            onValueChangedScriptTriggers.FirstOrDefault(trigger => trigger.VariableId == protocolVariable.Variable.Id),
            IsFileLogEnabled(protocolVariable.Variable));
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
                    variableEvents,
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
                    variableEvents,
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

    private void OnVariablePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ProjectConfigurationVariableWrapper.HasChanges))
        {
            return;
        }

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
    Presentations,
    Events,
    Scripts
}
