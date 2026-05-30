using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Qenex.QInsight.ViewModels.ViewableItem;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Variables.ValueConversion;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public class ConversionSeWrapper : PropertyChangedBase, IViewableItem
{
    public ConversionSeWrapper(IValConversion conversion)
    {
        Conversion = conversion;
    }

    #region UI Properties

    public IValConversion Conversion { get => field; init { field = value; OnPropertyChanged(); } }

    public string Label
    {
        get => Conversion.Name;
        set { Conversion.Name = value; OnPropertyChanged(); }
    }

    public FontWeight LabelWeight => FontWeights.Normal;

    public Visibility ToolTipVisibility => Visibility.Visible;
    public string ToolTip => GetToolTip();

    public BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SolutionExplorer/Presentation.png");
    public bool IsExpanded { get; set; }
    public ObservableCollection<IViewableItem> Children { get; set; } = [];

    public Dictionary<string, object>? CustomTags { get; set; } = [];

    #endregion

    public void Refresh()
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(ToolTip));
    }

    private string GetToolTip()
    {
        var sb = new StringBuilder();
        sb.Append("Conversion:");
        sb.Append(Environment.NewLine);
        sb.Append($"Name\t{Conversion.Name}");

        if (Conversion is LinearValConversion linearConversion)
        {
            sb.Append(Environment.NewLine);
            sb.Append($"Multiplier\t{linearConversion.Multiplier}");
            sb.Append(Environment.NewLine);
            sb.Append($"Offset\t{linearConversion.Offset}");
        }
        else if (Conversion is EnumValConversion enumConversion)
        {
            foreach (var enumValue in enumConversion.Enums)
            {
                sb.Append(Environment.NewLine);
                sb.Append($"{enumValue.Name}\t{enumValue.Value}");
            }
        }

        return sb.ToString();
    }
}
