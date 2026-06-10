using Qenex.QLibs.QUI;
using Qenex.QSuite.Drivers.Driver;

namespace Qenex.QInsight.ViewModels;

public class DriverPropertiesViewModel : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    private readonly IDriverBase driver;

    public DriverPropertiesViewModel(EventAggregator ea, IDriverBase driver)
    {
        _ = ea;
        this.driver = driver;
    }

    public string Name => driver.Specification.Name;
    public string Label => driver.Specification.Label;
    public string Description => driver.Specification.Description;
    public string Version => driver.Specification.Version?.ToString() ?? string.Empty;
    public string Author => driver.Specification.Author ?? string.Empty;
    public string Company => driver.Specification.Company ?? string.Empty;
    public string CreatedOn => driver.Specification.CreatedOn.ToString("yyyy/MM/dd");
    public bool IsEnabled => driver.IsEnabled;
    public string State => driver.State.ToString();
    public string StateMessage => driver.StateMessage ?? string.Empty;
    public int ProtocolCount => driver.Protocols.Count;

    public void RefreshDriver()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(Version));
        OnPropertyChanged(nameof(Author));
        OnPropertyChanged(nameof(Company));
        OnPropertyChanged(nameof(CreatedOn));
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(StateMessage));
        OnPropertyChanged(nameof(ProtocolCount));
    }
}
