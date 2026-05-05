using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QInsight.ViewModels.ModelWrappers;
using Qenex.QInsight.ViewModels.ViewableItem;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.TelerikDocking;
using Qenex.QSuite.Common.WpfComm;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class ScriptSeWrapper : PropertyChangedBase, IViewableItem
{
    public ScriptSeWrapper(ScriptWrapper scrWrapper)
    {
        ScriptWrapper = scrWrapper;
        Label = ScriptWrapper.FileName;
        
        if (ScriptWrapper is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ScriptWrapper.FileName))
                {
                    Label = ScriptWrapper.FileName;
                }
            };
        }
    }
    
    public ScriptWrapper ScriptWrapper { get; set; }

    #region UI Properties

    public string Label
    {
        get;//$"{scriptViewModel.FileName}";// ({scriptViewModel.AverageExecutionInterval:0.00} ms)";
        set
        {
            field = value;
            OnPropertyChanged();
		}
	}

    public double AverageExecutionInterval
    {
        get => ScriptWrapper.AverageExecutionInterval;
        set
        {
            ScriptWrapper.AverageExecutionInterval = value;
            // Update the label to reflect the new average execution interval
            OnPropertyChanged();
            OnPropertyChanged(nameof(Label));
		}
    }

    public FontWeight LabelWeight => FontWeights.Normal;

    public Visibility ToolTipVisibility => Visibility.Visible;
    public string ToolTip => GetToolTip();

    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/ScriptFile.png");
    public ObservableCollection<IViewableItem> Children { get; set; } = [];

    public Dictionary<string, object>? CustomTags { get; set; } = [];

    #endregion

    private string GetToolTip()
    {
        var sb = new StringBuilder();
        sb.Append("Script:");
        sb.Append(Environment.NewLine);
        sb.Append($"File\t{ScriptWrapper.FileName}");
        sb.Append(Environment.NewLine);
        sb.Append($"Last ex.\t{ScriptWrapper.LastExecutionInterval:0.00} ms");
        sb.Append(Environment.NewLine);
        sb.Append($"Avg. ex.\t{ScriptWrapper.AverageExecutionInterval:0.00} ms");
        sb.Append(Environment.NewLine);
        sb.Append($"Max ex.\t{ScriptWrapper.MaxExecutionInterval:0.00} ms");
        sb.Append(Environment.NewLine);

        return sb.ToString();
    }
}