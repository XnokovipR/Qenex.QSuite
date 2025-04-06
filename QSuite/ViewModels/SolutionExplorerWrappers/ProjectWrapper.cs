using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Modules.Module;
using Qenex.QSuite.ViewModels.ViewableItem;

namespace Qenex.QSuite.ViewModels.SolutionExplorerWrappers;

public class ProjectWrapper(IModuleBase prjModule) : PropertyChangedBase, IViewableItem
{
    #region UI Properties

    public string Label
    {
        get => prjModule.Specification.Label;
        set { prjModule.Specification.Label = value; OnPropertyChanged(); }
    }
    
    public string ToolTip => prjModule.Specification.Description;
    
    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/Project.png");
    public ObservableCollection<IViewableItem> Children { get; set; } = [];

    #endregion
}