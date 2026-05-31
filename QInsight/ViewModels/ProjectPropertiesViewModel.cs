using System.Collections.ObjectModel;
using System.Globalization;
using Qenex.QInsight.ViewModels.ModelWrappers;
using Qenex.QInsight.ViewModels.SolutionExplorerWrappers;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Modules.Module;

namespace Qenex.QInsight.ViewModels;

public class ProjectPropertiesViewModel : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    private readonly IModuleBase module;
    private readonly Action refreshSource;
    private readonly Func<bool> getIsEditProjectEnabled;
    private readonly Action<bool> setIsEditProjectEnabled;

    public ProjectPropertiesViewModel(
        EventAggregator ea,
        ProjectSeWrapper projectWrapper,
        Func<bool> getIsEditProjectEnabled,
        Action<bool> setIsEditProjectEnabled)
    {
        _ = ea;
        module = projectWrapper.PrjModule;
        refreshSource = projectWrapper.Refresh;
        this.getIsEditProjectEnabled = getIsEditProjectEnabled;
        this.setIsEditProjectEnabled = setIsEditProjectEnabled;
        Properties = CreateProperties();
    }

    public ObservableCollection<EditablePropertyWrapper> Properties { get; }

    public bool IsEditProjectEnabled
    {
        get => getIsEditProjectEnabled();
        set
        {
            if (getIsEditProjectEnabled() == value)
            {
                return;
            }

            setIsEditProjectEnabled(value);
            OnPropertyChanged();
            RefreshReadOnly();
        }
    }

    private ObservableCollection<EditablePropertyWrapper> CreateProperties()
    {
        return
        [
            Create("Project", "Label", () => module.Specification.Label, value => module.Specification.Label = value),
            Create("Project", "Description", () => module.Specification.Description, value => module.Specification.Description = value),
            Create("Project", "Version", () => module.Specification.Version?.ToString() ?? string.Empty, value => module.Specification.Version = ParseVersion(value)),
            Create("Project", "Author", () => module.Specification.Author ?? string.Empty, value => module.Specification.Author = value),
            Create("Project", "Company", () => module.Specification.Company ?? string.Empty, value => module.Specification.Company = value),
            Create("Project", "Creation Date", () => FormatDateTime(module.Specification.CreatedOn), value => module.Specification.CreatedOn = ParseDateTime(value)),
            Create("Project", "Modified", () => FormatDateTime(module.Specification.Modified), value => module.Specification.Modified = ParseDateTime(value))
        ];
    }

    private EditablePropertyWrapper Create(
        string group,
        string name,
        Func<object?> getValue,
        Action<string> setValue)
    {
        return new EditablePropertyWrapper(group, name, getValue, setValue, RefreshProjectProperties, null, IsPropertyReadOnly);
    }

    private bool IsPropertyReadOnly()
    {
        return !getIsEditProjectEnabled();
    }

    private void RefreshProjectProperties()
    {
        refreshSource();
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

    private static Version ParseVersion(string value)
    {
        return Version.TryParse(value, out var version)
            ? version
            : throw new InvalidOperationException($"Invalid project version \"{value}\".");
    }

    private static DateTime ParseDateTime(string value)
    {
        if (DateTime.TryParseExact(
                value,
                DateTimeFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var exactDateTime))
        {
            return exactDateTime;
        }

        if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var currentCultureDateTime))
        {
            return currentCultureDateTime;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var invariantDateTime))
        {
            return invariantDateTime;
        }

        throw new InvalidOperationException($"Invalid date/time \"{value}\". Use {DateTimeFormat}.");
    }

    private static string FormatDateTime(DateTime value)
    {
        return value == default ? string.Empty : value.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
    }
}
