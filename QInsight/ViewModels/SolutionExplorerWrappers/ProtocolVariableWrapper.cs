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
    private readonly IProtocolVariable protocolVariable;

    public ProtocolVariableWrapper(IProtocolVariable protVariable)
    {
        protocolVariable = protVariable;
        Children = [];
        
    }
    #region UI Properties
    

    public string Label
    {
        get => $"{protocolVariable.Variable.Label} ({protocolVariable.Variable.Id})";
        set { protocolVariable.Variable.Label = $"{value} ({protocolVariable.Variable.Id})"; OnPropertyChanged(); }
    }

    public Visibility ToolTipVisibility => Visibility.Visible;
    public string ToolTip => GetToolTip();
    
    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/Variable.png");
    public ObservableCollection<IViewableItem> Children { get; set; }

    #endregion
    
    private string GetToolTip()
    {
        var sb = new StringBuilder();
        sb.Append("Variable:");
        sb.Append(Environment.NewLine);
        sb.Append($"Label\t{protocolVariable.Variable.Label}");
        sb.Append(Environment.NewLine);
        sb.Append($"Name\t{protocolVariable.Variable.Name}");
        sb.Append(Environment.NewLine);
        sb.Append($"Descr.\t{protocolVariable.Variable.Description}");
        sb.Append(Environment.NewLine);

        if (protocolVariable.Variable is ScalarVariable scalarVariable)
        {
            sb.Append($"Size\t{scalarVariable.Size}");
            sb.Append(Environment.NewLine);
            sb.Append($"Type\t{scalarVariable.Values.ValueType.ToString()}");
        }
        
        return sb.ToString();
    }
}