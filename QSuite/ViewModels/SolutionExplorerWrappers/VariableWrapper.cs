using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QSuite.ViewModels.ViewableItem;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;


namespace Qenex.QSuite.ViewModels.SolutionExplorerWrappers;

public class VariableWrapper : PropertyChangedBase, IViewableItem
{
    private readonly IVariableBase variable;

    public VariableWrapper(IVariableBase variableToAdd)
    {
        variable = variableToAdd;
        Children = [];
        
    }
    #region UI Properties
    
    public string Label
    {
        get => $"{variable.Label} ({variable.Id})";
        set { variable.Label = $"{value} ({variable.Id})"; OnPropertyChanged(); }
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
        sb.Append($"Label\t{variable.Label}");
        sb.Append(Environment.NewLine);
        sb.Append($"Name\t{variable.Name}");
        sb.Append(Environment.NewLine);
        sb.Append($"Descr.\t{variable.Description}");
        sb.Append(Environment.NewLine);

        if (variable is ScalarVariable scalarVariable)
        {
            sb.Append($"Size\t{scalarVariable.Size}");
            sb.Append(Environment.NewLine);
            sb.Append($"Type\t{scalarVariable.Values.ValueType.ToString()}");
        }
        
        return sb.ToString();
    }
}