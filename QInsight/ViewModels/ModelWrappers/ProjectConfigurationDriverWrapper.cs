using System.Text;
using System.Collections.ObjectModel;
using Qenex.QInsight.Helpers;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Drivers.Driver;

namespace Qenex.QInsight.ViewModels.ModelWrappers;

public class ProjectConfigurationDriverWrapper(IDriverBase driver, bool isNew = false) : PropertyChangedBase
{
    private const string FileDataLoggerDriverName = "FileDataLoggerDriver";
    private const string FileDataReplayDriverName = "FileDataReplayDriver";
    private const string FileDataReplayDisplayLabel = "File data replay";
    private bool originalIsEnabled = driver.IsEnabled;
    private bool isEnabled = driver.IsEnabled;
    private string originalLabel = driver.Label;
    private string label = driver.Label;
    private string originalSettings = driver.RawSettings;
    private string settings = driver.RawSettings;

    public IDriverBase Driver => driver;
    public bool IsNew => isNew;
    public ObservableCollection<ProjectConfigurationLoadedProtocolWrapper> Protocols { get; } = new(
        driver.Protocols.Select(protocol => new ProjectConfigurationLoadedProtocolWrapper(protocol)));

    public string Name => driver.Specification.Name;
    public string Version => driver.Specification.Version.ToDisplayString();

    public string Label
    {
        get
        {
            if (IsFileDataReplayDriver)
            {
                return FileDataReplayDisplayLabel;
            }

            return label;
        }
        set
        {
            if (!CanEditLabel)
            {
                return;
            }

            value ??= string.Empty;
            if (label == value)
            {
                return;
            }

            label = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public string ToolTip
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("Driver:");
            sb.Append(Environment.NewLine);
            sb.Append($"Name\t{driver.Specification.Name}");
            sb.Append(Environment.NewLine);
            sb.Append($"Label\t{driver.Specification.Label}");
            sb.Append(Environment.NewLine);
            sb.Append($"Desc.\t{driver.Specification.Description}");
            sb.Append(Environment.NewLine);
            sb.Append($"Version\t{driver.Specification.Version.ToDisplayString()}");
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

    public bool CanEditLabel =>
        !IsFileDataLoggerDriver && !IsFileDataReplayDriver;

    public string Settings
    {
        get => settings;
        set
        {
            value ??= string.Empty;
            if (settings == value)
            {
                return;
            }

            settings = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public bool HasChanges => label != originalLabel
                              || (CanEditIsEnabled && isEnabled != originalIsEnabled)
                              || settings != originalSettings;

    public void ApplyChanges()
    {
        if (label != originalLabel)
        {
            driver.Label = label;
            originalLabel = label;
        }

        if (CanEditIsEnabled)
        {
            driver.IsEnabled = isEnabled;
            originalIsEnabled = isEnabled;
        }

        if (settings != originalSettings)
        {
            var previousSettings = driver.RawSettings;
            driver.RawSettings = settings;
            try
            {
                driver.SetConfiguration();
            }
            catch
            {
                driver.RawSettings = previousSettings;
                driver.SetConfiguration();
                throw;
            }
            originalSettings = settings;
        }

        OnPropertyChanged(nameof(HasChanges));
    }

    public void CancelChanges()
    {
        if (label != originalLabel)
        {
            label = originalLabel;
            OnPropertyChanged(nameof(Label));
        }

        if (CanEditIsEnabled && isEnabled != originalIsEnabled)
        {
            isEnabled = originalIsEnabled;
            OnPropertyChanged(nameof(IsEnabled));
        }

        if (settings != originalSettings)
        {
            settings = originalSettings;
            OnPropertyChanged(nameof(Settings));
        }

        OnPropertyChanged(nameof(HasChanges));
    }

    private bool IsFileDataReplayDriver =>
        driver.Specification.Name.Equals(FileDataReplayDriverName, StringComparison.OrdinalIgnoreCase);

    private bool IsFileDataLoggerDriver =>
        driver.Specification.Name.Equals(FileDataLoggerDriverName, StringComparison.OrdinalIgnoreCase);
}
