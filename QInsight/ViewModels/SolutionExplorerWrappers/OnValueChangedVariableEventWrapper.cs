using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Variables.VariableEvents;
using Qenex.QInsight.ViewModels.ViewableItem;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class OnValueChangedVariableEventWrapper : PropertyChangedBase, IViewableItem
{
    public OnValueChangedVariableEventWrapper(OnValueChangedVarEvent variableEvent)
    {
        VariableEvent = variableEvent;
    }
    
    #region UI Properties
    
    public OnValueChangedVarEvent VariableEvent {get => field; init { field = value; OnPropertyChanged(); } }

    public string Label
    {
        get => VariableEvent.Name;
        set { VariableEvent.Name = value; OnPropertyChanged(); }
    }
    
    public FontWeight LabelWeight => FontWeights.Normal;

    public Visibility ToolTipVisibility => Visibility.Visible;
    public string ToolTip
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("Threshold-Variable Event:");
            sb.Append(Environment.NewLine);
            sb.Append($"Label\t{VariableEvent.Name}");
            sb.Append(Environment.NewLine);
            sb.Append($"TH\t{VariableEvent.Threshold}");
            
            return sb.ToString();
        }
    }

    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/ValueChangedEvent.png");
    public ObservableCollection<IViewableItem> Children { get; set; } = new();

    public Dictionary<string, object>? CustomTags { get; set; } = new();

    #endregion
}