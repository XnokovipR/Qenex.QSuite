using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QInsight.ViewModels.ViewableItem;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Drivers.Driver;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class DriverSeWrapper : PropertyChangedBase, IViewableItem
{
    public DriverSeWrapper(IDriverBase driver)
    {
        Driver = driver;
    }
    #region UI Properties

    public IDriverBase Driver { get => field; init {field = value; OnPropertyChanged(); }}
    
    public string Label
    {
        get => Driver.Specification.Label;
        set { Driver.Specification.Label = value; OnPropertyChanged(); }
    }

    public FontWeight LabelWeight => FontWeights.SemiBold;

    public Visibility ToolTipVisibility => Visibility.Visible;
    public string ToolTip
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("Driver:");
            sb.Append(Environment.NewLine);
            sb.Append($"Label\t{Driver.Specification.Label}");
            sb.Append(Environment.NewLine);
            sb.Append($"Desc.\t{Driver.Specification.Description}");
            sb.Append(Environment.NewLine);
            sb.Append($"Version\t{Driver.Specification.Version}");
            sb.Append(Environment.NewLine);
            sb.Append($"Author\t{Driver.Specification.Author}");
            sb.Append(Environment.NewLine);
            sb.Append($"Co.\t{Driver.Specification.Company}");
            
            return sb.ToString();
        }
    }

    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/Driver.png");
    public ObservableCollection<IViewableItem> Children { get; set; } = [];

    public Dictionary<string, object>? CustomTags { get; set; } = [];

    #endregion
    
}