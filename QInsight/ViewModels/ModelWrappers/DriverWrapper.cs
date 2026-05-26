using System.Text;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Drivers.Driver;

namespace Qenex.QInsight.ViewModels.ModelWrappers;

public class DriverWrapper(IDriverBase driver) : PropertyChangedBase
{
    public IDriverBase Driver => driver;

    public string Label
    {
        get => string.IsNullOrWhiteSpace(driver.Label) ? driver.Specification.Label : driver.Label;
        set
        {
            if (driver.Label == value) return;
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
        get => driver.IsEnabled;
        set
        {
            if (driver.IsEnabled == value) return;
            driver.IsEnabled = value;
            OnPropertyChanged();
        }
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(ToolTip));
        OnPropertyChanged(nameof(IsEnabled));
    }
}
