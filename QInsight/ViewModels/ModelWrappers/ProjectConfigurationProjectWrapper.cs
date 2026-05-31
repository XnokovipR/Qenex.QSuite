using System.Globalization;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Modules.Module;

namespace Qenex.QInsight.ViewModels.ModelWrappers;

public class ProjectConfigurationProjectWrapper : PropertyChangedBase
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    private ProjectState originalState;
    private ProjectState currentState;

    public ProjectConfigurationProjectWrapper(IModuleBase module)
    {
        Module = module;
        originalState = ProjectState.FromModule(module);
        currentState = originalState;
        SetCreationDateNowCommand = new RelayCommand<object>(_ => SetCreationDate(DateTime.Now));
        SetModifiedNowCommand = new RelayCommand<object>(_ => SetModified(DateTime.Now));
    }

    public IModuleBase Module { get; }
    public RelayCommand<object> SetCreationDateNowCommand { get; }
    public RelayCommand<object> SetModifiedNowCommand { get; }

    public string Name => currentState.Name;

    public string Label
    {
        get => currentState.Label;
        set => UpdateState(currentState with { Label = value ?? string.Empty });
    }

    public string Description
    {
        get => currentState.Description;
        set => UpdateState(currentState with { Description = value ?? string.Empty });
    }

    public string Version
    {
        get => currentState.Version;
        set => UpdateState(currentState with { Version = value ?? string.Empty });
    }

    public string Author
    {
        get => currentState.Author;
        set => UpdateState(currentState with { Author = value ?? string.Empty });
    }

    public string Company
    {
        get => currentState.Company;
        set => UpdateState(currentState with { Company = value ?? string.Empty });
    }

    public string CreationDate
    {
        get => FormatDateTime(currentState.CreationDate);
        set => UpdateDate(value, date => currentState with { CreationDate = date });
    }

    public string Modified
    {
        get => FormatDateTime(currentState.Modified);
        set => UpdateDate(value, date => currentState with { Modified = date });
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
    public bool HasChanges => !currentState.Equals(originalState);

    public void MarkModifiedNow()
    {
        SetModified(DateTime.Now);
    }

    public void ApplyChanges()
    {
        if (!System.Version.TryParse(currentState.Version, out var version))
        {
            SetError($"Invalid project version \"{currentState.Version}\".");
            throw new InvalidOperationException(ErrorMessage);
        }

        Module.Specification.Label = currentState.Label;
        Module.Specification.Description = currentState.Description;
        Module.Specification.Version = version;
        Module.Specification.Author = currentState.Author;
        Module.Specification.Company = currentState.Company;
        Module.Specification.CreatedOn = currentState.CreationDate;
        Module.Specification.Modified = currentState.Modified;

        originalState = currentState;
        SetError(string.Empty);
        NotifyAllChanged();
    }

    public void CancelChanges()
    {
        currentState = originalState;
        SetError(string.Empty);
        NotifyAllChanged();
    }

    private void SetCreationDate(DateTime value)
    {
        UpdateState(currentState with { CreationDate = value });
        OnPropertyChanged(nameof(CreationDate));
    }

    private void SetModified(DateTime value)
    {
        UpdateState(currentState with { Modified = value });
        OnPropertyChanged(nameof(Modified));
    }

    private void UpdateDate(string value, Func<DateTime, ProjectState> update)
    {
        if (!TryParseDateTime(value, out var parsedDate))
        {
            SetError($"Invalid date/time \"{value}\". Use {DateTimeFormat}.");
            return;
        }

        SetError(string.Empty);
        UpdateState(update(parsedDate));
    }

    private void UpdateState(ProjectState state)
    {
        currentState = state;
        NotifyAllChanged();
    }

    private void NotifyAllChanged()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(Version));
        OnPropertyChanged(nameof(Author));
        OnPropertyChanged(nameof(Company));
        OnPropertyChanged(nameof(CreationDate));
        OnPropertyChanged(nameof(Modified));
        OnPropertyChanged(nameof(HasChanges));
    }

    private void SetError(string value)
    {
        ErrorMessage = value;
    }

    private static string FormatDateTime(DateTime value)
    {
        return value == default ? string.Empty : value.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
    }

    private static bool TryParseDateTime(string value, out DateTime dateTime)
    {
        return DateTime.TryParseExact(
                   value,
                   DateTimeFormat,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out dateTime)
               || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out dateTime)
               || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime);
    }

    private sealed record ProjectState(
        string Name,
        string Label,
        string Description,
        string Version,
        string Author,
        string Company,
        DateTime CreationDate,
        DateTime Modified)
    {
        public static ProjectState FromModule(IModuleBase module)
        {
            return new ProjectState(
                module.Specification.Name,
                module.Specification.Label,
                module.Specification.Description,
                module.Specification.Version?.ToString() ?? string.Empty,
                module.Specification.Author ?? string.Empty,
                module.Specification.Company ?? string.Empty,
                module.Specification.CreatedOn,
                module.Specification.Modified);
        }
    }
}
