using System.Collections.ObjectModel;
using Qenex.QLibs.QUI;

namespace Qenex.QInsight.ViewModels;

public class ProjectConfigurationViewModel : PropertyChangedBase
{
    public ProjectConfigurationViewModel()
    {
        NavigationItems =
        [
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.Variables, "Variables"),
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.Presentations, "Presentations"),
            new ProjectConfigurationNavigationItem(ProjectConfigurationSection.Events, "Events")
        ];

        SelectedNavigationItem = NavigationItems[0];
    }

    public ObservableCollection<ProjectConfigurationNavigationItem> NavigationItems { get; }

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
}

public sealed record ProjectConfigurationNavigationItem(ProjectConfigurationSection Section, string Label);

public enum ProjectConfigurationSection
{
    Variables,
    Presentations,
    Events
}
