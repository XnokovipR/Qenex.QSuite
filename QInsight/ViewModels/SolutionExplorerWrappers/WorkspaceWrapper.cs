using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Variables.ValuePresentation;
using Qenex.QInsight.ViewModels.ViewableItem;
using Qenex.QLibs.QUI.TelerikDocking;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class WorkspaceWrapper(IWorkspaceViewModel workspaceViewModel) : PropertyChangedBase, IViewableItem
{
    #region UI Properties
    

    public string Label
    {
        get => workspaceViewModel.WinTitle;
        set { workspaceViewModel.WinTitle = value; OnPropertyChanged(); }
    }
    
    public FontWeight LabelWeight => FontWeights.Normal;

    public Visibility ToolTipVisibility => Visibility.Visible;
    public string ToolTip => GetToolTip();
    
    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/Presentation.png");
    public ObservableCollection<IViewableItem> Children { get; set; } = [];

    public Dictionary<string, object>? CustomTags { get; set; } = new();

    #endregion
    
    private string GetToolTip()
    {
        var sb = new StringBuilder();
        sb.Append("Workspace:");
        sb.Append(Environment.NewLine);
        sb.Append($"Header\t{workspaceViewModel.WinTitle}");
        sb.Append(Environment.NewLine);
        sb.Append($"Name\t{workspaceViewModel.Name}");

        return sb.ToString();
    }
}