using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QInsight.ViewModels.ViewableItem;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;


namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class ProtocolVariableWrapper : PropertyChangedBase, IViewableItem
{
    public ProtocolVariableWrapper(IProtocolVariable protocolVariable)
    {
        ProtocolVariable = protocolVariable;
    }
    #region UI Properties
    public IProtocolVariable ProtocolVariable { get => field; init { field = value; OnPropertyChanged(); } }

    public string Label
    {
        get => $"{ProtocolVariable.Variable.Label} ({ProtocolVariable.Variable.Id})";
        set { ProtocolVariable.Variable.Label = $"{value} ({ProtocolVariable.Variable.Id})"; OnPropertyChanged(); }
    }
    
    public FontWeight LabelWeight => FontWeights.Normal;

    public Visibility ToolTipVisibility => Visibility.Visible;
    public string ToolTip => GetToolTip();
    
    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/Variable.png");
    public ObservableCollection<IViewableItem> Children { get; set; } = [];
    public Dictionary<string, object>? CustomTags { get; set; } = [];

    #endregion
    
    private string GetToolTip()
    {
        var sb = new StringBuilder();
        sb.Append("Variable:");
        sb.Append(Environment.NewLine);
        sb.Append($"Label\t{ProtocolVariable.Variable.Label}");
        sb.Append(Environment.NewLine);
        sb.Append($"Name\t{ProtocolVariable.Variable.Name}");
        sb.Append(Environment.NewLine);
        sb.Append($"Descr.\t{ProtocolVariable.Variable.Description}");
        sb.Append(Environment.NewLine);

        if (ProtocolVariable.Variable is ScalarVariable scalarVariable)
        {
            sb.Append($"Size\t{scalarVariable.Size}");
            sb.Append(Environment.NewLine);
            sb.Append($"Type\t{scalarVariable.Values.ValueType.ToString()}");
        }
        
        return sb.ToString();
    }
}