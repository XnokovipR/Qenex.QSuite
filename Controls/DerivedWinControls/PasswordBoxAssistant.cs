using System.Windows;
using System.Windows.Controls;

namespace Qenex.QSuite.Controls.OverridenWinControls;

/// <summary>
/// Makes PasswordBox.Password bindable (the control is sealed and its Password is not
/// a dependency property). Usage — both attached properties are required:
/// q:PasswordBoxAssistant.BindPassword="True"
/// q:PasswordBoxAssistant.BoundPassword="{Binding ..., Mode=TwoWay}".
/// BindPassword subscribes the box; it cannot be done from BoundPassword's change
/// callback because that never fires when the initial bound value equals the
/// property default (empty string).
/// </summary>
public static class PasswordBoxAssistant
{
    public static readonly DependencyProperty BindPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BindPassword", typeof(bool), typeof(PasswordBoxAssistant),
            new PropertyMetadata(false, OnBindPasswordChanged));

    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword", typeof(string), typeof(PasswordBoxAssistant),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating", typeof(bool), typeof(PasswordBoxAssistant), new PropertyMetadata(false));

    public static bool GetBindPassword(DependencyObject dependencyObject)
    {
        return (bool)dependencyObject.GetValue(BindPasswordProperty);
    }

    public static void SetBindPassword(DependencyObject dependencyObject, bool value)
    {
        dependencyObject.SetValue(BindPasswordProperty, value);
    }

    public static string GetBoundPassword(DependencyObject dependencyObject)
    {
        return (string)dependencyObject.GetValue(BoundPasswordProperty);
    }

    public static void SetBoundPassword(DependencyObject dependencyObject, string value)
    {
        dependencyObject.SetValue(BoundPasswordProperty, value);
    }

    private static void OnBindPasswordChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not PasswordBox passwordBox)
        {
            return;
        }

        if ((bool)e.OldValue)
        {
            passwordBox.PasswordChanged -= HandlePasswordChanged;
        }

        if ((bool)e.NewValue)
        {
            passwordBox.Password = GetBoundPassword(passwordBox);
            passwordBox.PasswordChanged += HandlePasswordChanged;
        }
    }

    private static void OnBoundPasswordChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not PasswordBox passwordBox || (bool)passwordBox.GetValue(IsUpdatingProperty))
        {
            return;
        }

        passwordBox.Password = e.NewValue as string ?? string.Empty;
    }

    private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
    {
        var passwordBox = (PasswordBox)sender;
        passwordBox.SetValue(IsUpdatingProperty, true);
        SetBoundPassword(passwordBox, passwordBox.Password);
        passwordBox.SetValue(IsUpdatingProperty, false);
    }
}
