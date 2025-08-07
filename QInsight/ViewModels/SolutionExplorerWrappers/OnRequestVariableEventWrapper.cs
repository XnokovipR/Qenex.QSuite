using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Variables.VariableEvents;
using Qenex.QInsight.ViewModels.ViewableItem;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class OnRequestVariableEventWrapper : PropertyChangedBase, IViewableItem
{
    private readonly OnRequestVarEvent variableEvent;
    
    public OnRequestVariableEventWrapper(OnRequestVarEvent varEvent)
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
    
    public FontWeight LabelWeight => FontWeights.Normal;

    public Visibility ToolTipVisibility => Visibility.Visible;
    public string ToolTip
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("On-Request-Variable Event:");
            sb.Append(Environment.NewLine);
            sb.Append($"Label\t{variableEvent.Name}");
            
            return sb.ToString();
        }
    }

    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/RequestEvent.png");
    public ObservableCollection<IViewableItem> Children { get; set; }

    #endregion
}