using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Docking;

namespace Qenex.QInsight.ViewModels;

public class QRadPane : RadPane
{
    public QRadPane()
    {
        this.DefaultStyleKey = typeof(QRadPane);
    }

    public Dictionary<string, object> CustomTags
    {
        get => (Dictionary<string, object>)GetValue(CustomTagsProperty);
        set => SetValue(CustomTagsProperty, value);
    }
    public static readonly DependencyProperty CustomTagsProperty =
        DependencyProperty.Register(
            nameof(CustomTags),
            typeof(Dictionary<string, object>),
            typeof(QRadPane),
            new PropertyMetadata(null));
}