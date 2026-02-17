using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QInsight.ViewModels.ViewableItem;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Scripts.Script;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class ScriptWrapper : PropertyChangedBase, IViewableItem
{
    public ScriptWrapper(IScriptBase script)
    {
        Script = script;
    }

    #region UI Properties

    public string Label
    {
        get => $"{Script.FileName} ({Script.AverageExecutionInterval:0.00} ms)";
        set
        {
            Script.FileName = $"{value} ({Script.AverageExecutionInterval:0.00} ms)";
            OnPropertyChanged();
        }
    }

    public double AverageExecutionInterval
    {
        get => Script.AverageExecutionInterval;
        set
        {
            Script.AverageExecutionInterval = value;
            // Update the label to reflect the new average execution interval
            Label = Script.FileName;
            OnPropertyChanged();
        }
    }

    public IScriptBase Script { get; set; }

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
        sb.Append($"File\t{Script.FileName}");
        sb.Append(Environment.NewLine);
        sb.Append($"Last ex.\t{Script.LastExecutionInterval:0.00} ms");
        sb.Append(Environment.NewLine);
        sb.Append($"Avg. ex.\t{Script.AverageExecutionInterval:0.00} ms");
        sb.Append(Environment.NewLine);
        sb.Append($"Max ex.\t{Script.AverageExecutionInterval:0.00} ms");
        sb.Append(Environment.NewLine);

        return sb.ToString();
    }
}