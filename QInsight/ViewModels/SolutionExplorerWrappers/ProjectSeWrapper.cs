using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Modules.Module;
using Qenex.QInsight.ViewModels.ViewableItem;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class ProjectSeWrapper : PropertyChangedBase, IViewableItem
{
    public ProjectSeWrapper(IModuleBase prjModule)
    {
        PrjModule = prjModule;
    }
    
    #region UI Properties
    
    public IModuleBase PrjModule { get => field; init { field = value; OnPropertyChanged(); } }

    public string Label
    {
        get => PrjModule.Specification.Label;
        set { PrjModule.Specification.Label = value; OnPropertyChanged(); }
    }
    
    public FontWeight LabelWeight => FontWeights.Bold;
    
    public Visibility ToolTipVisibility => Visibility.Visible;
 
    public string ToolTip
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("Project details:");
            sb.Append(Environment.NewLine);
            sb.Append($"Label\t{PrjModule.Specification.Label}");
            sb.Append(Environment.NewLine);
            sb.Append($"Descr.\t{PrjModule.Specification.Description}");
            sb.Append(Environment.NewLine);
            sb.Append($"Ver.\t{PrjModule.Specification.Version}");
            sb.Append(Environment.NewLine);
            sb.Append($"Author\t{PrjModule.Specification.Author}");
            sb.Append(Environment.NewLine);
            sb.Append($"Co.\t{PrjModule.Specification.Company}");
            sb.Append(Environment.NewLine);
            sb.Append($"Created\t{PrjModule.Specification.CreatedOn}");
            sb.Append(Environment.NewLine);
            sb.Append($"Modified\t{PrjModule.Specification.Modified}");
            
            
            return sb.ToString();
        }    
    }
    
    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/Project.png");
    public bool IsExpanded { get; set; }
    public ObservableCollection<IViewableItem> Children { get; set; } = [];

    public Dictionary<string, object>? CustomTags { get; set; } = [];

    #endregion
}
