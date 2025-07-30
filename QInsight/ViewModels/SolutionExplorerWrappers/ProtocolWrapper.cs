using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QInsight.ViewModels.ViewableItem;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class ProtocolWrapper : PropertyChangedBase, IViewableItem
{
    private readonly IProtocolBase protocol;

    public ProtocolWrapper(IProtocolBase prot)
    {
        protocol = prot;
        Children = [];
        
    }
    #region UI Properties



    public string Label
    {
        get => protocol.Specification.Label;
        set { protocol.Specification.Label = value; OnPropertyChanged(); }
    }
    
    public Visibility ToolTipVisibility => Visibility.Visible;
    public string ToolTip
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("Protocol:");
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
    
    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/Protocol.png");
    public ObservableCollection<IViewableItem> Children { get; set; }

    #endregion
    
}