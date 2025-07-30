using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;


namespace Qenex.QInsight.ViewModels.ViewableItem;

public interface IViewableItem
{
    string Label { get; set; }
    string ToolTip { get; }
    Visibility ToolTipVisibility { get; }
    BitmapImage Icon { get; }
    ObservableCollection<IViewableItem> Children { get; set; }
}