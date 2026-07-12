using System.Text;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QInsight.ViewModels.ModelWrappers;

public class ProjectConfigurationLoadedProtocolWrapper(IProtocolBase protocol, bool isNew = false) : PropertyChangedBase
{
    private bool originalIsEnabled = protocol.IsEnabled;
    private bool isEnabled = protocol.IsEnabled;
    private string originalSettings = protocol.RawSettings;
    private string settings = protocol.RawSettings;

    public IProtocolBase Protocol => protocol;
    public bool IsNew => isNew;

    public string Label => string.IsNullOrWhiteSpace(protocol.Specification.Label)
        ? protocol.Specification.Name
        : protocol.Specification.Label;

    public string Name => protocol.Specification.Name;
    public string Version => protocol.Specification.Version?.ToString() ?? string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(protocol.Specification.Label)
        ? protocol.Specification.Name
        : protocol.Specification.Label;

    public string ToolTip
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("Protocol:");
            sb.Append(Environment.NewLine);
            sb.Append($"Name\t{protocol.Specification.Name}");
            sb.Append(Environment.NewLine);
            sb.Append($"Label\t{protocol.Specification.Label}");
            sb.Append(Environment.NewLine);
            sb.Append($"Desc.\t{protocol.Specification.Description}");
            sb.Append(Environment.NewLine);
            sb.Append($"Version\t{protocol.Specification.Version}");
            sb.Append(Environment.NewLine);
            sb.Append($"Author\t{protocol.Specification.Author}");
            sb.Append(Environment.NewLine);
            sb.Append($"Co.\t{protocol.Specification.Company}");

            return sb.ToString();
        }
    }

    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            if (isEnabled == value)
            {
                return;
            }

            isEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

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

    public bool HasChanges => isEnabled != originalIsEnabled || settings != originalSettings;

    public void ApplyChanges()
    {
        protocol.IsEnabled = isEnabled;
        originalIsEnabled = isEnabled;

        if (settings != originalSettings)
        {
            // Same pattern as the driver wrapper: a protocol that rejects its settings
            // (e.g. Modbus without 'mode') keeps its previous working configuration.
            var previousSettings = protocol.RawSettings;
            protocol.RawSettings = settings;
            try
            {
                protocol.SetConfiguration();
            }
            catch
            {
                protocol.RawSettings = previousSettings;
                protocol.SetConfiguration();
                throw;
            }
            originalSettings = settings;
        }

        OnPropertyChanged(nameof(HasChanges));
    }

    public void CancelChanges()
    {
        if (isEnabled != originalIsEnabled)
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
}
