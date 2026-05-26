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

public class VariableSeWrapper : PropertyChangedBase, IViewableItem
{
    public VariableSeWrapper(IVariableBase variable)
    {
        Variable = variable;
    }
    #region UI Properties
    
    public string Label
    {
        get => $"{Variable.Label} ({Variable.Id})";
        set { Variable.Label = $"{value} ({Variable.Id})"; OnPropertyChanged(); }
    }

    public IVariableBase Variable { get; set; }
    
    public FontWeight LabelWeight => FontWeights.Normal;

    public Visibility ToolTipVisibility => Visibility.Visible;
    public string ToolTip => GetToolTip();
    
    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/Variable.png");
    public bool IsExpanded { get; set; }
    public ObservableCollection<IViewableItem> Children { get; set; } = [];

    public Dictionary<string, object>? CustomTags { get; set; } = [];

    #endregion
    
    private string GetToolTip()
    {
        var sb = new StringBuilder();
        sb.Append("Variable:");
        sb.Append(Environment.NewLine);
        sb.Append($"Label\t{Variable.Label}");
        sb.Append(Environment.NewLine);
        sb.Append($"Name\t{Variable.Name}");
        sb.Append(Environment.NewLine);
        sb.Append($"Descr.\t{Variable.Description}");
        sb.Append(Environment.NewLine);

        if (Variable is ScalarVariable scalarVariable)
        {
            sb.Append($"Size\t{scalarVariable.Size}");
            sb.Append(Environment.NewLine);
            sb.Append($"Type\t{scalarVariable.Values.ValueType.ToString()}");
        }
        
        return sb.ToString();
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(ToolTip));
    }
}
