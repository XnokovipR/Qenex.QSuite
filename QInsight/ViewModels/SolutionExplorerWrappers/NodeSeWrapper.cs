using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QInsight.ViewModels.ViewableItem;
using Qenex.QSuite.Common.WpfComm;
using Telerik.Windows.Diagrams.Core;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class NodeSeWrapper : IViewableItem
{
    //private readonly NodeType nodeType;
    private readonly string prefix;
    private readonly string suffix;
    

    public NodeSeWrapper(NodeType nType, string labelPrefix = "", string labelSuffix = "")
    {
        prefix = labelPrefix;
        suffix = labelSuffix;
        TypeOfNode = nType;
        Children = [];
        CustomTags = [];
    }

    #region UI Properties
    
    public string Label
    {
        get => TypeOfNode == NodeType.OnlyPrefixFolder ? prefix : $"{prefix} {TypeOfNode.ToString()} {suffix}"; 
        set { }
    }
    
    public FontWeight LabelWeight => FontWeights.SemiBold;

    public Visibility ToolTipVisibility => Visibility.Hidden;
    public string ToolTip => string.Empty;
    
    public NodeType TypeOfNode { get; }
    
    public BitmapImage Icon => ImageGetter.GetBitmapImage($"Icons/SolutionExplorer/{GetBitmapImageName(TypeOfNode)}");
    public bool IsExpanded { get; set; }
    public ObservableCollection<IViewableItem> Children { get; set; }
    
    public Dictionary<string, object>? CustomTags { get; set; }

    #endregion
    
    private string GetBitmapImageName(NodeType typeOfNode)
    {
        return typeOfNode switch
        {
            NodeType.Scripts => "ScriptFolder.png",
            NodeType.Drivers => "Drivers.png",
            NodeType.Protocols => "Protocols.png",
            NodeType.Variables => "Variables.png",
            NodeType.Events => "Events.png",
            NodeType.Presentations => "Presentations.png",
            NodeType.OnlyPrefixFolder => "VariableFolder.png",
            NodeType.Workspaces => "Workspaces.png",
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
        Workspaces,
        Scripts
    }

    
}
