using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
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

    public Visibility ToolTipVisibility => Visibility.Visible;
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

    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/Driver.png");
    public ObservableCollection<IViewableItem> Children { get; set; }

    #endregion
    
}