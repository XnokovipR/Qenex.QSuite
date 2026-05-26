using System.Collections.ObjectModel;
using Qenex.QInsight.ViewModels.ModelWrappers;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Drivers.Driver;

namespace Qenex.QInsight.ViewModels;

public class CommunicatedDriversPropertiesViewModel : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    public CommunicatedDriversPropertiesViewModel(EventAggregator ea, IEnumerable<IDriverBase> drivers)
    {
        _ = ea;
        Drivers = new ObservableCollection<DriverWrapper>(
            drivers
                .Where(driver => !driver.Specification.Name.Equals("FileDataReplayDriver", StringComparison.OrdinalIgnoreCase))
                .Select(driver => new DriverWrapper(driver)));
    }

    public ObservableCollection<DriverWrapper> Drivers { get; }

    public void RefreshDrivers()
    {
        foreach (var driver in Drivers)
        {
            driver.Refresh();
        }
    }
}
