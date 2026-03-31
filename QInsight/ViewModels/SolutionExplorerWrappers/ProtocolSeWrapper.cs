using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QInsight.ViewModels.ViewableItem;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class ProtocolSeWrapper : PropertyChangedBase, IViewableItem
{
    public ProtocolSeWrapper(IProtocolBase protocol)
    {
        Protocol = protocol;
    }
    
    #region UI Properties
    
    public IProtocolBase Protocol { get => field; init {field = value; OnPropertyChanged(); }}
    
    public string Label
    {
        get => Protocol.Specification.Label;
        set { Protocol.Specification.Label = value; OnPropertyChanged(); }
    }
    
    public FontWeight LabelWeight => FontWeights.SemiBold;
    
    public Visibility ToolTipVisibility => Visibility.Visible;
    public string ToolTip
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("Protocol:");
            sb.Append(Environment.NewLine);
            sb.Append($"Label\t{Protocol.Specification.Label}");
            sb.Append(Environment.NewLine);
            sb.Append($"Desc.\t{Protocol.Specification.Description}");
            sb.Append(Environment.NewLine);
            sb.Append($"Version\t{Protocol.Specification.Version}");
            sb.Append(Environment.NewLine);
            sb.Append($"Author\t{Protocol.Specification.Author}");
            sb.Append(Environment.NewLine);
            sb.Append($"Co.\t{Protocol.Specification.Company}");
            
            return sb.ToString();
        }
    }
    
    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/Protocol.png");
    public ObservableCollection<IViewableItem> Children { get; set; } = [];

    public Dictionary<string, object>? CustomTags { get; set; } = [];

    #endregion
    
}