using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Variables.VariableEvents;
using Qenex.QSuite.ViewModels.ViewableItem;

namespace Qenex.QSuite.ViewModels.SolutionExplorerWrappers;

public class OnValueChangedVariableEventWrapper : PropertyChangedBase, IViewableItem
{
    private readonly OnValueChangedVarEvent variableEvent;
    
    public OnValueChangedVariableEventWrapper(OnValueChangedVarEvent varEvent)
    {
        variableEvent = varEvent;
        Children = new ObservableCollection<IViewableItem>();
    }
    
    #region UI Properties
    

    public string Label
    {
        get => variableEvent.Name;
        set { variableEvent.Name = value; OnPropertyChanged(); }
    }

    public Visibility ToolTipVisibility => Visibility.Visible;
    public string ToolTip
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("Threshold-Variable Event:");
            sb.Append(Environment.NewLine);
            sb.Append($"Label\t{variableEvent.Name}");
            sb.Append(Environment.NewLine);
            sb.Append($"TH\t{variableEvent.Threshold}");
            
            return sb.ToString();
        }
    }

    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/ValueChangedEvent.png");
    public ObservableCollection<IViewableItem> Children { get; set; }

    #endregion
}