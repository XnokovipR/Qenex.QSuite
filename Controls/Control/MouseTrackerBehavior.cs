using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Qenex.QSuite.Controls.Control;

public static class MouseTrackerBehavior
{
    public static readonly DependencyProperty TrackProperty =
        DependencyProperty.RegisterAttached(
            "Track",
            typeof(bool),
            typeof(MouseTrackerBehavior),
            new PropertyMetadata(false, OnTrackChanged));

    public static void SetTrack(DependencyObject obj, bool value)
        => obj.SetValue(TrackProperty, value);

    public static bool GetTrack(DependencyObject obj)
        => (bool)obj.GetValue(TrackProperty);

    private static void OnTrackChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ContentControl control)
        {
            if ((bool)e.NewValue)
                CompositionTarget.Rendering += (_, __) =>
                {
                    if (control.DataContext is IHasMousePosition vm)
                    {
                        var pos = Mouse.GetPosition(control);
                        vm.MousePosition = pos;
                    }
                };
        }
    }
}