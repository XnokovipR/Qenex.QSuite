using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Qenex.QSuite.ViewModels.ViewableItem;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Drivers.Driver;

namespace Qenex.QSuite.ViewModels.SolutionExplorerWrappers;

public class DriverWrapper : PropertyChangedBase, IViewableItem
{
    private readonly IDriverBase driver;

    public DriverWrapper(IDriverBase drv)
    {
        driver = drv;
        Children = [];
        
    }
    #region UI Properties
    

    public string Label
    {
        get => driver.Specification.Label;
        set { driver.Specification.Label = value; OnPropertyChanged(); }
    }

    public string ToolTip => driver.Specification.Description;
    
    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/Driver.png");
    public ObservableCollection<IViewableItem> Children { get; set; }

    #endregion
    
}