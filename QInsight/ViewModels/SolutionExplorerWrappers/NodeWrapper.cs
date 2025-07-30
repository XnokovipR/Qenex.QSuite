using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QInsight.ViewModels.ViewableItem;
using Qenex.QSuite.Common.WpfComm;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class NodeWrapper : IViewableItem
{
    private readonly NodeType nodeType;
    private readonly string prefix;
    private readonly string suffix;
    

    public NodeWrapper(NodeType nType, string labelPrefix = "", string labelSuffix = "")
    {
        prefix = labelPrefix;
        suffix = labelSuffix;
        nodeType = nType;
        Children = [];
    }

    #region UI Properties
    
    public string Label
    {
        get => nodeType == NodeType.OnlyPrefixFolder ? prefix : $"{prefix} {nodeType.ToString()} {suffix}"; 
        set { }
    }

    public Visibility ToolTipVisibility => Visibility.Hidden;
    public string ToolTip => string.Empty;
    
    public BitmapImage Icon => ImageGetter.GetBitmapImage($"Icons/SolutionExplorer/{GetBitmapImageName(nodeType)}");
    public ObservableCollection<IViewableItem> Children { get; set; }

    #endregion
    
    private string GetBitmapImageName(NodeType typeOfNode)
    {
        return typeOfNode switch
        {
            NodeType.Drivers => "Drivers.png",
            NodeType.Protocols => "Protocols.png",
            NodeType.Variables => "Variables.png",
            NodeType.Events => "Events.png",
            NodeType.Presentations => "Presentations.png",
            NodeType.OnlyPrefixFolder => "VariableFolder.png",
            
            _ => "Drivers.png"
        };
    }
    
    public enum NodeType
    {
        OnlyPrefixFolder,
        Drivers,
        Protocols,
        Variables,
        Events,
        Presentations,
    }

    
}