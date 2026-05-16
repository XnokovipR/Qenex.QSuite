using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Qenex.QInsight.ViewModels.ModelWrappers;
using Qenex.QInsight.ViewModels.ViewableItem;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.TelerikDocking;
using Qenex.QSuite.Common.WpfComm;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class ScriptSeWrapper : PropertyChangedBase, IViewableItem
{
    private readonly Dispatcher dispatcher;

    public ScriptSeWrapper(ScriptWrapper scrWrapper)
    {
        dispatcher = Dispatcher.CurrentDispatcher;
        ScriptWrapper = scrWrapper;
        Label = ScriptWrapper.FileName;
        
        if (ScriptWrapper is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged += (_, e) => OnScriptWrapperPropertyChanged(e.PropertyName);
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
            OnPropertyChanged();
            OnPropertyChanged(nameof(Label));
            OnPropertyChanged(nameof(ToolTip));
		}
    }

    public double LastExecutionInterval
    {
        get => ScriptWrapper.LastExecutionInterval;
        set
        {
            ScriptWrapper.LastExecutionInterval = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ToolTip));
        }
    }

    public double MaxExecutionInterval
    {
        get => ScriptWrapper.MaxExecutionInterval;
        set
        {
            ScriptWrapper.MaxExecutionInterval = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ToolTip));
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
        sb.Append("Script execution statistics");
        sb.Append(Environment.NewLine);
        sb.Append($"Last\t{ScriptWrapper.LastExecutionInterval:0.00} ms");
        sb.Append(Environment.NewLine);
        sb.Append($"Avg.\t{ScriptWrapper.AverageExecutionInterval:0.00} ms");
        sb.Append(Environment.NewLine);
        sb.Append($"Max\t{ScriptWrapper.MaxExecutionInterval:0.00} ms");
        sb.Append(Environment.NewLine);

        return sb.ToString();
    }

    private void OnScriptWrapperPropertyChanged(string? propertyName)
    {
        if (!dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => OnScriptWrapperPropertyChanged(propertyName));
            return;
        }

        if (propertyName == nameof(ScriptWrapper.FileName))
        {
            Label = ScriptWrapper.FileName;
        }
        else if (propertyName is nameof(ScriptWrapper.LastExecutionInterval)
                 or nameof(ScriptWrapper.AverageExecutionInterval)
                 or nameof(ScriptWrapper.MaxExecutionInterval))
        {
            OnPropertyChanged(nameof(LastExecutionInterval));
            OnPropertyChanged(nameof(AverageExecutionInterval));
            OnPropertyChanged(nameof(MaxExecutionInterval));
            OnPropertyChanged(nameof(ToolTip));
        }
    }
}
