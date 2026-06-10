using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using Qenex.QLibs.QUI;
using Qenex.QInsight.Models.Project;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QInsight.ViewModels;

public class VariableWatchViewModel : WorkspaceViewModelBase
{
    private readonly List<IProtocolVariable> subscribedProtocolVariables = [];

    public VariableWatchViewModel(EventAggregator ea) : base(ea)
    {
        Variables = [];
    }

    public ObservableCollection<VariableWatchItemViewModel> Variables { get; }

    public void Refresh(RealProjectData? projectData)
    {
        UnsubscribeValueChanges();
        Variables.Clear();

        if (projectData?.Module == null)
        {
            return;
        }

        var loggedVariableIds = GetLoggedVariableIds(projectData.Module.Drivers);
        foreach (var variable in projectData.Module.Variables
                     .OrderBy(variable => variable.Namespace)
                     .ThenBy(variable => variable.Label)
                     .ThenBy(variable => variable.Name)
                     .ThenBy(variable => variable.Id))
        {
            Variables.Add(new VariableWatchItemViewModel(variable, loggedVariableIds.Contains(variable.Id)));
        }

        SubscribeValueChanges(projectData.Module.Drivers);
    }

    public override void Clean()
    {
        UnsubscribeValueChanges();
        Variables.Clear();
    }

    public override Task CleanAsync(CancellationToken ct = default)
    {
        Clean();
        return Task.CompletedTask;
    }

    public override string Header { get; set; } = "";
    public override string Name { get; set; } = $"WorkspaceViewModel_VariableWatch_{Guid.NewGuid().ToString().Replace("-", "_")}";
    public override string WinTitle { get => field; set { field = value; OnPropertyChanged(); } } = "Variable Watch";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Workspace;
    public override bool IsDocument => true;

    private void SubscribeValueChanges(IEnumerable<IDriverBase> drivers)
    {
        foreach (var protocolVariable in drivers
                     .SelectMany(driver => driver.Protocols)
                     .SelectMany(protocol => protocol.Variables)
                     .Distinct())
        {
            protocolVariable.SubscribeValueChanged(OnProtocolVariableValueChanged);
            protocolVariable.SubscribeAsyncValueChanged(OnProtocolVariableValueChangedAsync);
            subscribedProtocolVariables.Add(protocolVariable);
        }
    }

    private void UnsubscribeValueChanges()
    {
        foreach (var protocolVariable in subscribedProtocolVariables)
        {
            protocolVariable.UnsubscribeValueChanged(OnProtocolVariableValueChanged);
            protocolVariable.UnsubscribeAsyncValueChanged(OnProtocolVariableValueChangedAsync);
        }

        subscribedProtocolVariables.Clear();
    }

    private void OnProtocolVariableValueChanged()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher?.CheckAccess() == false)
        {
            dispatcher.BeginInvoke(RefreshValues);
            return;
        }

        RefreshValues();
    }

    private Task OnProtocolVariableValueChangedAsync(IProtocolVariable protocolVariable)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher?.CheckAccess() == false)
        {
            return dispatcher
                .InvokeAsync(RefreshValues)
                .Task;
        }

        RefreshValues();
        return Task.CompletedTask;
    }

    private void RefreshValues()
    {
        foreach (var variable in Variables)
        {
            variable.RefreshValue();
        }
    }

    private static HashSet<int> GetLoggedVariableIds(IEnumerable<IDriverBase> drivers)
    {
        return drivers
            .Where(driver => driver.Specification.Name.Equals("FileDataLoggerDriver", StringComparison.OrdinalIgnoreCase))
            .SelectMany(driver => driver.Protocols)
            .SelectMany(protocol => protocol.Variables)
            .Select(protocolVariable => protocolVariable.Variable.Id)
            .ToHashSet();
    }
}

public class VariableWatchItemViewModel : PropertyChangedBase
{
    private readonly IVariableBase variable;

    public VariableWatchItemViewModel(IVariableBase variable, bool logged)
    {
        this.variable = variable;
        Logged = logged;
    }

    public string Namespace => variable.Namespace;
    public string Label => variable.Label;
    public string Name => variable.Name;
    public int Id => variable.Id;
    public string Value => FormatValue(variable.GetValue());
    public bool Logged { get; }

    public void RefreshValue()
    {
        OnPropertyChanged(nameof(Value));
    }

    private static string FormatValue(object? value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
