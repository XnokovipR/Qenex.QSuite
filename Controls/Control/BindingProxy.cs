using System.Windows;

namespace Qenex.QSuite.Controls.Control;

/// <summary>
/// Freezable proxy carrying the DataContext to XAML objects outside the visual tree
/// (e.g. Telerik GridView columns, which do not inherit DataContext). Declare as a
/// resource with Data="{Binding}" and bind via Source={StaticResource ...}, Path=Data.*.
/// </summary>
public sealed class BindingProxy : Freezable
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(object), typeof(BindingProxy), new PropertyMetadata(null));

    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override Freezable CreateInstanceCore() => new BindingProxy();
}
