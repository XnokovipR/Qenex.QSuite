using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Qenex.QSuite.ViewModels.ViewableItem;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.ViewModels.SolutionExplorerWrappers;

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
    
    public string ToolTip => protocol.Specification.Description;
    
    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/Protocol.png");
    public ObservableCollection<IViewableItem> Children { get; set; }

    #endregion
    
}