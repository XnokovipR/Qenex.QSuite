using System.Text;
using System.Collections.ObjectModel;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Drivers.Driver;

namespace Qenex.QInsight.ViewModels.ModelWrappers;

public class ProjectConfigurationDriverWrapper(IDriverBase driver, bool isNew = false) : PropertyChangedBase
{
    private const string FileDataReplayDriverName = "FileDataReplayDriver";
    private const string FileDataReplayDisplayLabel = "File data replay";
    private bool originalIsEnabled = driver.IsEnabled;
    private bool isEnabled = driver.IsEnabled;

    public IDriverBase Driver => driver;
    public bool IsNew => isNew;
    public ObservableCollection<ProjectConfigurationLoadedProtocolWrapper> Protocols { get; } = new(
        driver.Protocols.Select(protocol => new ProjectConfigurationLoadedProtocolWrapper(protocol)));

    public string Label
    {
        get
        {
            if (IsFileDataReplayDriver)
            {
                return FileDataReplayDisplayLabel;
            }

            return string.IsNullOrWhiteSpace(driver.Label) ? driver.Specification.Label : driver.Label;
        }
        set
        {
            if (driver.Label == value)
            {
                return;
            }

            driver.Label = value;
            OnPropertyChanged();
        }
    }

    public string ToolTip
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("Driver:");
            sb.Append(Environment.NewLine);
            sb.Append($"Label\t{driver.Specification.Label}");
            sb.Append(Environment.NewLine);
            sb.Append($"Desc.\t{driver.Specification.Description}");
            sb.Append(Environment.NewLine);
            sb.Append($"Version\t{driver.Specification.Version}");
            sb.Append(Environment.NewLine);
            sb.Append($"Author\t{driver.Specification.Author}");
            sb.Append(Environment.NewLine);
            sb.Append($"Co.\t{driver.Specification.Company}");

            return sb.ToString();
        }
    }

    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            if (!CanEditIsEnabled || isEnabled == value)
            {
                return;
            }

            isEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public bool CanEditIsEnabled =>
        !IsFileDataReplayDriver;

    public bool HasChanges => CanEditIsEnabled && isEnabled != originalIsEnabled;

    public void ApplyChanges()
    {
        if (!CanEditIsEnabled)
        {
            return;
        }

        driver.IsEnabled = isEnabled;
        originalIsEnabled = isEnabled;
        OnPropertyChanged(nameof(HasChanges));
    }

    public void CancelChanges()
    {
        if (!CanEditIsEnabled || isEnabled == originalIsEnabled)
        {
            return;
        }

        isEnabled = originalIsEnabled;
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(HasChanges));
    }

    private bool IsFileDataReplayDriver =>
        driver.Specification.Name.Equals(FileDataReplayDriverName, StringComparison.OrdinalIgnoreCase);
}
