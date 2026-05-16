using System.Windows;
using System.Windows.Controls;
using Qenex.QSuite.Controls.OverridenWinControls;

namespace Qenex.QInsight.Views;

public static class TextBoxAutoScrollBehavior
{
    public static readonly DependencyProperty AutoScrollProperty =
        DependencyProperty.RegisterAttached(
            "AutoScroll",
            typeof(bool),
            typeof(TextBoxAutoScrollBehavior),
            new PropertyMetadata(false, OnAutoScrollChanged));

    public static bool GetAutoScroll(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoScrollProperty);
    }

    public static void SetAutoScroll(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoScrollProperty, value);
    }

    private static void OnAutoScrollChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is not QTextBox textBox)
            return;

        if ((bool)e.NewValue)
        {
            textBox.TextChanged += TextBox_TextChanged;
        }
        else
        {
            textBox.TextChanged -= TextBox_TextChanged;
        }
    }

    private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is QTextBox textBox)
        {
            textBox.ScrollToEnd();
        }
    }
}