using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Modules.Module;
using Qenex.QInsight.ViewModels.ViewableItem;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class ProjectWrapper(IModuleBase prjModule) : PropertyChangedBase, IViewableItem
{
    #region UI Properties

    public string Label
    {
        get => prjModule.Specification.Label;
        set { prjModule.Specification.Label = value; OnPropertyChanged(); }
    }
    
    public Visibility ToolTipVisibility => Visibility.Visible;
 
    public string ToolTip
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("Project details:");
            sb.Append(Environment.NewLine);
            sb.Append($"Label\t{prjModule.Specification.Label}");
            sb.Append(Environment.NewLine);
            sb.Append($"Descr.\t{prjModule.Specification.Description}");
            sb.Append(Environment.NewLine);
            sb.Append($"Ver.\t{prjModule.Specification.Version}");
            sb.Append(Environment.NewLine);
            sb.Append($"Author\t{prjModule.Specification.Author}");
            sb.Append(Environment.NewLine);
            sb.Append($"Co.\t{prjModule.Specification.Company}");
            
            
            return sb.ToString();
        }    
    }
    
    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/Project.png");
    public ObservableCollection<IViewableItem> Children { get; set; } = [];

    #endregion
}