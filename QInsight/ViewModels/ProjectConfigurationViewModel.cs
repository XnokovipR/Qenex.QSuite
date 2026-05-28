using System.Collections.ObjectModel;
using System.ComponentModel;
using Qenex.QInsight.EventAggregatorMsgs;
using Qenex.QInsight.ViewModels.ModelWrappers;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.Scripting.Script;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace Qenex.QInsight.ViewModels;

public class ProjectConfigurationViewModel : PropertyChangedBase
{
    private readonly EventAggregator? eventAggregator;
    private RadWindow? parentWindow;

    public ProjectConfigurationViewModel()
        : this(null, [], [])
    {
    }

    public ProjectConfigurationViewModel(
        EventAggregator? eventAggregator,
        IEnumerable<IDriverBase> drivers,
        IEnumerable<IScriptBase> scripts)
    {
        this.eventAggregator = eventAggregator;
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
        Scripts = new ObservableCollection<ProjectConfigurationScriptWrapper>(
            scripts.Select(script => new ProjectConfigurationScriptWrapper(script)));
        foreach (var script in Scripts)
        {
            script.PropertyChanged += OnScriptPropertyChanged;
        }

        ApplyCommand = new RelayCommand<object>(_ => ApplyChanges(), _ => HasChanges);
        CancelCommand = new RelayCommand<object>(_ => Cancel());
        SelectedNavigationItem = NavigationItems[0];
    }

    public ObservableCollection<ProjectConfigurationNavigationItem> NavigationItems { get; }
    public ObservableCollection<ProjectConfigurationDriverWrapper> Drivers { get; }
    public ObservableCollection<ProjectConfigurationScriptWrapper> Scripts { get; }
    public IEnumerable<EnumMemberViewModel> ExecutionModes { get; } = EnumDataSource.FromType<ScriptExecutionMode>();
    public RelayCommand<object> ApplyCommand { get; }
    public RelayCommand<object> CancelCommand { get; }

    public bool HasChanges =>
        Drivers.Any(driver => driver.HasChanges)
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
        }
    }

    public ProjectConfigurationSection SelectedSection => SelectedNavigationItem.Section;

    public void SelectSection(ProjectConfigurationSection section)
    {
        SelectedNavigationItem = NavigationItems.First(item => item.Section == section);
    }

    public void SetParentWindow(RadWindow window)
    {
        parentWindow = window;
    }

    private void ApplyChanges()
    {
        foreach (var driver in Drivers)
        {
            driver.ApplyChanges();
        }
        foreach (var script in Scripts)
        {
            script.ApplyChanges();
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
        foreach (var script in Scripts)
        {
            script.CancelChanges();
        }

        parentWindow?.Close();
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

        NotifyHasChangesChanged();
    }

    private void NotifyHasChangesChanged()
    {
        OnPropertyChanged(nameof(HasChanges));
        ApplyCommand.OnCanExecuteChanged();
    }
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
