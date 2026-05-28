using System.Collections.ObjectModel;
using System.ComponentModel;
using Qenex.QInsight.EventAggregatorMsgs;
using Qenex.QInsight.ViewModels.ModelWrappers;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Drivers.Driver;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.ViewModels;

public class ProjectConfigurationViewModel : PropertyChangedBase
{
    private readonly EventAggregator? eventAggregator;
    private RadWindow? parentWindow;

    public ProjectConfigurationViewModel()
        : this(null, [])
    {
    }

    public ProjectConfigurationViewModel(EventAggregator? eventAggregator, IEnumerable<IDriverBase> drivers)
    {
        this.eventAggregator = eventAggregator;
        NavigationItems =
        [
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.CommunicationDrivers, "Communication Drivers"),
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.Variables, "Variables"),
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.Presentations, "Presentations"),
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.Events, "Events")
        ];
        Drivers = new ObservableCollection<ProjectConfigurationDriverWrapper>(
            drivers.Select(driver => new ProjectConfigurationDriverWrapper(driver)));
        foreach (var driver in Drivers)
        {
            driver.PropertyChanged += OnDriverPropertyChanged;
        }

        ApplyCommand = new RelayCommand<object>(_ => ApplyChanges(), _ => HasChanges);
        CancelCommand = new RelayCommand<object>(_ => Cancel());
        SelectedNavigationItem = NavigationItems[0];
    }

    public ObservableCollection<ProjectConfigurationNavigationItem> NavigationItems { get; }
    public ObservableCollection<ProjectConfigurationDriverWrapper> Drivers { get; }
    public RelayCommand<object> ApplyCommand { get; }
    public RelayCommand<object> CancelCommand { get; }

    public bool HasChanges => Drivers.Any(driver => driver.HasChanges);

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

        eventAggregator?.Publish(new ProjectConfigurationAppliedMsg());
        NotifyHasChangesChanged();
    }

    private void Cancel()
    {
        foreach (var driver in Drivers)
        {
            driver.CancelChanges();
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
    Events
}
