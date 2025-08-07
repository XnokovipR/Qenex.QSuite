using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Variables.ValuePresentation;
using Qenex.QInsight.ViewModels.ViewableItem;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class PresentationWrapper: PropertyChangedBase, IViewableItem
{
    private readonly IPresentation presentation;

    public PresentationWrapper(IPresentation presentationToAdd)
    {
        presentation = presentationToAdd;
        Children = [];
        
    }
    #region UI Properties
    

    public string Label
    {
        get => presentation.Label;
        set { presentation.Label = value; OnPropertyChanged(); }
    }
    
    public FontWeight LabelWeight => FontWeights.Normal;

    public Visibility ToolTipVisibility => Visibility.Visible;
    public string ToolTip => GetToolTip();
    
    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/Presentation.png");
    public ObservableCollection<IViewableItem> Children { get; set; }

    #endregion
    
    private string GetToolTip()
    {
        var sb = new StringBuilder();
        sb.Append("Presentation:");
        sb.Append(Environment.NewLine);
        sb.Append($"Label\t{presentation.Label}");
        sb.Append(Environment.NewLine);
        sb.Append($"Conv.\t{presentation.Conversion.ToString()}");
        sb.Append(Environment.NewLine);
        sb.Append($"Min.\t{presentation.Min}");
        sb.Append(Environment.NewLine);
        sb.Append($"Max.\t{presentation.Max}");
        sb.Append(Environment.NewLine);
        sb.Append($"Format\t{presentation.PrintFormat}");
        sb.Append(Environment.NewLine);
        sb.Append($"Unit\t{presentation.Unit}");
        
        return sb.ToString();
    }
}